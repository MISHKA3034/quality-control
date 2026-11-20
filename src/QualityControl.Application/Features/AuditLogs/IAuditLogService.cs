using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.AuditLogs.Dtos;

namespace QualityControl.Application.Features.AuditLogs;

/// <summary>
/// Reads the audit trail. There is deliberately no write, update or delete method
/// here: entries are appended by <see cref="Common.Interfaces.IAuditService"/> and
/// by the persistence layer, and nothing may alter them afterwards.
/// </summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Full history for one entity, newest first — the "История" tab of a record.</summary>
    Task<IReadOnlyList<AuditLogDto>> GetEntityHistoryAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default);
}
