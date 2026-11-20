using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.CorrectiveActions;
using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Корректирующие действия по устранению дефектов.
/// </summary>
[ApiController]
[Route("api/corrective-actions")]
[Authorize]
[Produces("application/json")]
public class CorrectiveActionsController : ControllerBase
{
    private readonly ICorrectiveActionService _correctiveActionService;

    public CorrectiveActionsController(ICorrectiveActionService correctiveActionService)
    {
        _correctiveActionService = correctiveActionService;
    }

    /// <summary>Список корректирующих действий с поиском, фильтрацией и пагинацией.</summary>
    /// <remarks>
    /// Фильтры: дефект, партия, подразделение, ответственный, статус, критичность,
    /// признак просрочки, диапазон дат.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CorrectiveActionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CorrectiveActionListItemDto>>> GetPaged(
        [FromQuery] CorrectiveActionQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _correctiveActionService.GetPagedAsync(query, cancellationToken));

    /// <summary>Карточка корректирующего действия с историей изменений.</summary>
    /// <remarks>Поле allowedNextStatuses содержит статусы, доступные из текущего.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CorrectiveActionDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _correctiveActionService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание корректирующего действия по дефекту.</summary>
    [HttpPost]
    [Authorize(Roles =
        $"{RoleNames.Administrator},{RoleNames.QualityInspector},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CorrectiveActionDto>> Create(
        CreateCorrectiveActionRequest request,
        CancellationToken cancellationToken)
    {
        var action = await _correctiveActionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = action.Id }, action);
    }

    /// <summary>Изменение описания, срока или сведений о выполненных работах.</summary>
    /// <response code="409">Действие закрыто и не может быть изменено.</response>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CorrectiveActionDto>> Update(
        int id,
        UpdateCorrectiveActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _correctiveActionService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Назначение ответственного.</summary>
    [HttpPost("{id:int}/assign")]
    [Authorize(Roles =
        $"{RoleNames.Administrator},{RoleNames.QualityInspector},"
        + $"{RoleNames.WorkshopManager},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CorrectiveActionDto>> Assign(
        int id,
        AssignCorrectiveActionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _correctiveActionService.AssignAsync(id, request, cancellationToken));

    /// <summary>Смена статуса корректирующего действия.</summary>
    /// <remarks>Перевод в статус «Проверен» доступен только контролёру ОТК.</remarks>
    /// <response code="403">Недостаточно прав для запрошенного перехода.</response>
    /// <response code="409">Переход недопустим из текущего статуса.</response>
    [HttpPost("{id:int}/status")]
    [Authorize]
    [ProducesResponseType(typeof(CorrectiveActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CorrectiveActionDto>> ChangeStatus(
        int id,
        ChangeCorrectiveActionStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _correctiveActionService.ChangeStatusAsync(id, request, cancellationToken));
}
