using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductionBatches;
using QualityControl.Application.Features.ProductionBatches.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Производственные партии.
/// </summary>
[ApiController]
[Route("api/production-batches")]
[Authorize]
[Produces("application/json")]
public class ProductionBatchesController : ControllerBase
{
    private readonly IProductionBatchService _batchService;

    public ProductionBatchesController(IProductionBatchService batchService)
    {
        _batchService = batchService;
    }

    /// <summary>Список партий с поиском, фильтрацией, сортировкой и пагинацией.</summary>
    /// <remarks>
    /// Фильтры: изделие, участок, мастер, статус, диапазон дат изготовления.
    /// Сортировка: batchNumber, manufactureDate, product, department, quantity, status.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductionBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductionBatchListItemDto>>> GetPaged(
        [FromQuery] ProductionBatchQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _batchService.GetPagedAsync(query, cancellationToken));

    /// <summary>Партии, доступные для оформления акта проверки.</summary>
    [HttpGet("inspectable")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductionBatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductionBatchListItemDto>>> GetInspectable(
        CancellationToken cancellationToken) =>
        Ok(await _batchService.GetInspectableAsync(cancellationToken));

    /// <summary>Карточка партии.</summary>
    /// <remarks>Поле allowedNextStatuses содержит статусы, в которые партию можно перевести сейчас.</remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductionBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductionBatchDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _batchService.GetByIdAsync(id, cancellationToken));

    /// <summary>Регистрация партии.</summary>
    [HttpPost]
    [Authorize(Roles =
        $"{RoleNames.Administrator},{RoleNames.WorkshopManager},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductionBatchDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductionBatchDto>> Create(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await _batchService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
    }

    /// <summary>Изменение партии.</summary>
    /// <response code="409">Партия уже прошла контроль и не может быть изменена.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles =
        $"{RoleNames.Administrator},{RoleNames.WorkshopManager},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductionBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductionBatchDto>> Update(
        int id,
        UpdateProductionBatchRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _batchService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Смена статуса партии.</summary>
    /// <response code="409">Переход недопустим из текущего статуса.</response>
    [HttpPost("{id:int}/status")]
    [Authorize(Roles =
        $"{RoleNames.Administrator},{RoleNames.WorkshopManager},{RoleNames.ProductionManager}")]
    [ProducesResponseType(typeof(ProductionBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductionBatchDto>> ChangeStatus(
        int id,
        ChangeBatchStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _batchService.ChangeStatusAsync(id, request, cancellationToken));

    /// <summary>Удаление партии.</summary>
    /// <response code="409">По партии оформлены акты проверки.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _batchService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
