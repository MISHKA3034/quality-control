using Microsoft.Extensions.Logging;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;
using QualityControl.Infrastructure.Persistence;

namespace QualityControl.Infrastructure.Services;

/// <inheritdoc cref="IAuditService"/>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        ILogger<AuditService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditAction action,
        string? entityName = null,
        string? entityId = null,
        string? description = null,
        bool isSuccess = true,
        int? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new AuditLog
            {
                // Explicit identity wins: a failed sign-in knows who was attempted
                // while no one is authenticated yet.
                UserId = userId ?? _currentUser.UserId,
                UserName = userName ?? _currentUser.UserName,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                Timestamp = _clock.UtcNow,
                IpAddress = _currentUser.IpAddress,
                UserAgent = _currentUser.UserAgent,
                IsSuccess = isSuccess
            };

            _context.AuditLogs.Add(entry);

            // Shares the caller's context and therefore its transaction: an audit
            // entry for an operation that later rolls back must roll back with it.
            //
            // The consequence is that this flushes whatever else the caller has
            // pending. Call it after your SaveChangesAsync, not before, or you will
            // commit half-finished work under the audit entry's name.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Losing an audit entry must never fail the user's operation. Surface it
            // to the application log — where it becomes an ops problem, not a 500.
            _logger.LogError(
                ex,
                "Не удалось записать событие аудита {Action} для {EntityName}#{EntityId}.",
                action,
                entityName,
                entityId);
        }
    }
}
