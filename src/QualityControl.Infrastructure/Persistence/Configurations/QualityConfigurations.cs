using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityControl.Domain.Entities;

namespace QualityControl.Infrastructure.Persistence.Configurations;

public class ProductionBatchConfiguration : IEntityTypeConfiguration<ProductionBatch>
{
    public void Configure(EntityTypeBuilder<ProductionBatch> builder)
    {
        builder.ToTable("ProductionBatches");

        builder.Property(b => b.BatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(2000);

        // Stored as int, not string: the reports group and filter on it constantly.
        builder.Property(b => b.Status).HasConversion<int>();

        builder.HasIndex(b => b.BatchNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(b => b.Status);

        // The batch list is filtered by department and ordered by date by default.
        builder.HasIndex(b => new { b.DepartmentId, b.ManufactureDate });
        builder.HasIndex(b => b.ManufactureDate);

        builder.HasOne(b => b.Product)
            .WithMany(p => p.ProductionBatches)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Department)
            .WithMany(d => d.ProductionBatches)
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Foreman)
            .WithMany(e => e.SupervisedBatches)
            .HasForeignKey(b => b.ForemanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

public class QualityInspectionConfiguration : IEntityTypeConfiguration<QualityInspection>
{
    public void Configure(EntityTypeBuilder<QualityInspection> builder)
    {
        builder.ToTable("QualityInspections");

        builder.Property(i => i.InspectionNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Comment).HasMaxLength(2000);
        builder.Property(i => i.Outcome).HasConversion<int>();

        builder.Ignore(i => i.DefectRate);

        // Unique act numbers are what make the race in number generation safe:
        // a duplicate fails the transaction instead of being written.
        builder.HasIndex(i => i.InspectionNumber).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasIndex(i => i.InspectionDate);
        builder.HasIndex(i => new { i.ProductionBatchId, i.InspectionDate });
        builder.HasIndex(i => i.InspectorId);
        builder.HasIndex(i => i.Outcome);

        builder.HasOne(i => i.ProductionBatch)
            .WithMany(b => b.Inspections)
            .HasForeignKey(i => i.ProductionBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Inspector)
            .WithMany(e => e.Inspections)
            .HasForeignKey(i => i.InspectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}

public class InspectionResultConfiguration : IEntityTypeConfiguration<InspectionResult>
{
    public void Configure(EntityTypeBuilder<InspectionResult> builder)
    {
        builder.ToTable("InspectionResults");

        builder.Property(r => r.ParameterName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.ExpectedValue).HasMaxLength(200);
        builder.Property(r => r.ActualValue).HasMaxLength(200);
        builder.Property(r => r.UnitOfMeasure).HasMaxLength(20);
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.Outcome).HasConversion<int>();

        builder.HasIndex(r => r.QualityInspectionId);

        // Result lines are part of the act, not independent records: they are
        // replaced wholesale on edit and have no meaning without it.
        builder.HasOne(r => r.QualityInspection)
            .WithMany(i => i.Results)
            .HasForeignKey(r => r.QualityInspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mirrors the act's filter, so lines of a removed act stay hidden with it.
        builder.HasQueryFilter(r => !r.QualityInspection.IsDeleted);
    }
}

public class DefectTypeConfiguration : IEntityTypeConfiguration<DefectType>
{
    public void Configure(EntityTypeBuilder<DefectType> builder)
    {
        builder.ToTable("DefectTypes");

        builder.Property(t => t.Code).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.DefaultSeverity).HasConversion<int>();

        builder.HasIndex(t => t.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(t => t.IsActive);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

public class DefectConfiguration : IEntityTypeConfiguration<Defect>
{
    public void Configure(EntityTypeBuilder<Defect> builder)
    {
        builder.ToTable("Defects");

        builder.Property(d => d.Description).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.Location).HasMaxLength(300);
        builder.Property(d => d.Comment).HasMaxLength(1000);
        builder.Property(d => d.Severity).HasConversion<int>();

        builder.Ignore(d => d.IsOpen);

        builder.HasIndex(d => d.QualityInspectionId);
        builder.HasIndex(d => d.DefectTypeId);
        builder.HasIndex(d => d.Severity);
        builder.HasIndex(d => d.DetectedAt);

        builder.HasOne(d => d.QualityInspection)
            .WithMany(i => i.Defects)
            .HasForeignKey(d => d.QualityInspectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DefectType)
            .WithMany(t => t.Defects)
            .HasForeignKey(d => d.DefectTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => !d.IsDeleted && !d.QualityInspection.IsDeleted);
    }
}

public class DefectPhotoConfiguration : IEntityTypeConfiguration<DefectPhoto>
{
    public void Configure(EntityTypeBuilder<DefectPhoto> builder)
    {
        builder.ToTable("DefectPhotos");

        builder.Property(p => p.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(p => p.StoredFileName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Caption).HasMaxLength(500);

        builder.HasIndex(p => p.DefectId);
        builder.HasIndex(p => p.StoredFileName).IsUnique();

        builder.HasOne(p => p.Defect)
            .WithMany(d => d.Photos)
            .HasForeignKey(p => p.DefectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.UploadedByUser)
            .WithMany()
            .HasForeignKey(p => p.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(p => !p.Defect.IsDeleted && !p.Defect.QualityInspection.IsDeleted);
    }
}

public class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> builder)
    {
        builder.ToTable("CorrectiveActions");

        builder.Property(a => a.Description).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.ResolutionNotes).HasMaxLength(2000);
        builder.Property(a => a.Status).HasConversion<int>();

        builder.HasIndex(a => a.DefectId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.AssignedToId);

        // Drives the "overdue" filter and the dashboard counter.
        builder.HasIndex(a => new { a.Status, a.DueDate });

        builder.HasOne(a => a.Defect)
            .WithMany(d => d.CorrectiveActions)
            .HasForeignKey(a => a.DefectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedTo)
            .WithMany(e => e.AssignedCorrectiveActions)
            .HasForeignKey(a => a.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.VerifiedByEmployee)
            .WithMany()
            .HasForeignKey(a => a.VerifiedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted && !a.Defect.IsDeleted);
    }
}

public class CorrectiveActionHistoryConfiguration : IEntityTypeConfiguration<CorrectiveActionHistory>
{
    public void Configure(EntityTypeBuilder<CorrectiveActionHistory> builder)
    {
        builder.ToTable("CorrectiveActionHistory");

        builder.Property(h => h.Comment).HasMaxLength(1000);
        builder.Property(h => h.FromStatus).HasConversion<int?>();
        builder.Property(h => h.ToStatus).HasConversion<int>();

        builder.HasIndex(h => h.CorrectiveActionId);

        builder.HasOne(h => h.CorrectiveAction)
            .WithMany(a => a.History)
            .HasForeignKey(h => h.CorrectiveActionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)

            // The history is the record of who did what; it must not lose its author.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(h => !h.CorrectiveAction.IsDeleted);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(l => l.UserName).HasMaxLength(100);
        builder.Property(l => l.EntityName).HasMaxLength(100);
        builder.Property(l => l.EntityId).HasMaxLength(50);
        builder.Property(l => l.Description).HasMaxLength(1000);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(512);
        builder.Property(l => l.Action).HasConversion<int>();

        // JSON diff of arbitrary size; nvarchar(max).
        builder.Property(l => l.Changes);

        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.EntityName, l.EntityId });
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.Action);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)

            // The trail outlives the account: removing a user must not erase what
            // they did, so the reference is severed rather than cascaded.
            .OnDelete(DeleteBehavior.SetNull);

        // No query filter, deliberately. The audit log is append-only evidence and
        // must remain readable in full, including entries about deleted records.
    }
}
