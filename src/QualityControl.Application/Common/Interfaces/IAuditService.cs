using QualityControl.Domain.Enums;

namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Writes the audit trail. Entity create/update/delete is captured automatically by
/// the persistence layer; this interface is for actions that have no entity change
/// to observe — sign-in, export, an explicit business event.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Appends an audit record. Never throws: a failure to log must not fail the
    /// user's operation, so implementations log the error and move on.
    /// </summary>
    Task LogAsync(
        AuditAction action,
        string? entityName = null,
        string? entityId = null,
        string? description = null,
        bool isSuccess = true,
        int? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default);
}
