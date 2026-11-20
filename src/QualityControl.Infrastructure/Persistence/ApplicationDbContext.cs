using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Domain.Common;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Infrastructure.Persistence;

/// <summary>
/// The EF Core session. Beyond mapping, it enforces two cross-cutting rules on save:
/// audit fields are stamped centrally, and entity changes are written to the audit log.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUser;
    private readonly IDateTimeProvider? _clock;

    /// <summary>
    /// Used by <c>dotnet ef</c> at design time, where no request and no user exist.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
        : base(options)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductionBatch> ProductionBatches => Set<ProductionBatch>();

    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();

    public DbSet<InspectionResult> InspectionResults => Set<InspectionResult>();

    public DbSet<DefectType> DefectTypes => Set<DefectType>();

    public DbSet<Defect> Defects => Set<Defect>();

    public DbSet<DefectPhoto> DefectPhotos => Set<DefectPhoto>();

    public DbSet<CorrectiveAction> CorrectiveActions => Set<CorrectiveAction>();

    public DbSet<CorrectiveActionHistory> CorrectiveActionHistory => Set<CorrectiveActionHistory>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;
        var userId = _currentUser?.UserId;

        ApplyAuditFields(now, userId);
        ApplySoftDelete(now, userId);

        // Captured before saving, while the original values are still available;
        // written after, once generated keys exist.
        var auditEntries = CaptureAuditEntries();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (auditEntries.Count > 0)
        {
            await WriteAuditEntriesAsync(auditEntries, now, userId, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Stamps created/updated metadata so no use case has to remember to.
    /// </summary>
    private void ApplyAuditFields(DateTime now, int? userId)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedByUserId = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedByUserId = userId;

                    // Guards against a detached-update wiping the original values:
                    // whoever created the row is not this request's business.
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedByUserId).IsModified = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Turns deletes of <see cref="ISoftDeletable"/> entities into flag updates.
    /// Quality records are evidence and must survive a delete.
    /// </summary>
    private void ApplySoftDelete(DateTime now, int? userId)
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedByUserId = userId;
        }
    }

    /// <summary>
    /// Snapshots pending changes for the audit log.
    /// </summary>
    private List<PendingAuditEntry> CaptureAuditEntries()
    {
        var entries = new List<PendingAuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            // Never audit the audit log itself, and skip entities with nothing to say.
            if (entry.Entity is AuditLog
                || entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var entityName = entry.Entity.GetType().Name;
            var action = ResolveAction(entry);

            if (action is null)
            {
                continue;
            }

            entries.Add(new PendingAuditEntry(entry, entityName, action.Value, BuildChanges(entry, action.Value)));
        }

        return entries;
    }

    /// <summary>
    /// Maps an EF state to a business action, accounting for soft deletes having
    /// already been rewritten to Modified.
    /// </summary>
    private static AuditAction? ResolveAction(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => AuditAction.Create,
        EntityState.Deleted => AuditAction.Delete,
        EntityState.Modified when entry.Entity is ISoftDeletable { IsDeleted: true }
            && entry.Property(nameof(ISoftDeletable.IsDeleted)).IsModified => AuditAction.Delete,
        EntityState.Modified => AuditAction.Update,
        _ => null
    };

    /// <summary>
    /// Serialises what actually changed. Property values can hold secrets, so the
    /// sensitive ones are named and excluded rather than filtered by guesswork.
    /// </summary>
    private static string? BuildChanges(EntityEntry entry, AuditAction action)
    {
        if (action is AuditAction.Delete)
        {
            return null;
        }

        var changes = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;

            if (SensitiveProperties.Contains(name))
            {
                continue;
            }

            if (action is AuditAction.Create)
            {
                if (property.CurrentValue is not null)
                {
                    changes[name] = property.CurrentValue;
                }

                continue;
            }

            if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            {
                changes[name] = new { Old = property.OriginalValue, New = property.CurrentValue };
            }
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }

    private async Task WriteAuditEntriesAsync(
        List<PendingAuditEntry> entries,
        DateTime now,
        int? userId,
        CancellationToken cancellationToken)
    {
        foreach (var pending in entries)
        {
            // Read the key after saving: an inserted row has no id before that.
            var entityId = pending.Entry.Metadata.FindPrimaryKey()?.Properties
                .Select(p => pending.Entry.Property(p.Name).CurrentValue?.ToString())
                .FirstOrDefault();

            AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserName = _currentUser?.UserName,
                Action = pending.Action,
                EntityName = pending.EntityName,
                EntityId = entityId,
                Changes = pending.Changes,
                Description = $"{pending.Action} {pending.EntityName}",
                Timestamp = now,
                IpAddress = _currentUser?.IpAddress,
                UserAgent = _currentUser?.UserAgent,
                IsSuccess = true
            });
        }

        // Second save, for the log rows only. They are appended after the fact and
        // never modify the entities that were just written.
        await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Properties whose values must never reach the audit log.
    /// </summary>
    private static readonly HashSet<string> SensitiveProperties =
    [
        nameof(User.PasswordHash),
        nameof(RefreshToken.TokenHash),
        nameof(RefreshToken.ReplacedByTokenHash),
        nameof(PasswordResetToken.TokenHash)
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,

        // Cyrillic must stay readable in the log rather than become \uXXXX escapes.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private sealed record PendingAuditEntry(
        EntityEntry Entry,
        string EntityName,
        AuditAction Action,
        string? Changes);
}
