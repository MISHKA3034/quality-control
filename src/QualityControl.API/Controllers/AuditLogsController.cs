using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.AuditLogs;
using QualityControl.Application.Features.AuditLogs.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Журнал действий пользователей.
/// </summary>
/// <remarks>
/// Только чтение. Записи журнала добавляются автоматически и не подлежат изменению
/// или удалению — иначе он перестанет быть доказательной базой.
/// </remarks>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.ProductionManager}")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>Журнал с фильтрацией и пагинацией.</summary>
    /// <remarks>Фильтры: пользователь, действие, сущность, признак успеха, диапазон дат.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetPaged(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _auditLogService.GetPagedAsync(query, cancellationToken));

    /// <summary>История изменений конкретной записи.</summary>
    /// <param name="entityName">Имя сущности, например Product.</param>
    /// <param name="entityId">Идентификатор записи.</param>
    [HttpGet("entity/{entityName}/{entityId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetEntityHistory(
        string entityName,
        string entityId,
        CancellationToken cancellationToken) =>
        Ok(await _auditLogService.GetEntityHistoryAsync(entityName, entityId, cancellationToken));
}
