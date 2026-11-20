using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.QualityInspections;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Акты проверки качества.
/// </summary>
[ApiController]
[Route("api/inspections")]
[Authorize]
[Produces("application/json")]
public class QualityInspectionsController : ControllerBase
{
    private readonly IQualityInspectionService _inspectionService;

    public QualityInspectionsController(IQualityInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    /// <summary>Список актов с поиском, фильтрацией, сортировкой и пагинацией.</summary>
    /// <remarks>
    /// Фильтры: партия, изделие, контролёр, подразделение, результат, вид дефекта, диапазон дат.
    /// Сортировка: inspectionNumber, inspectionDate, batchNumber, product, inspector, outcome.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<QualityInspectionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<QualityInspectionListItemDto>>> GetPaged(
        [FromQuery] QualityInspectionQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _inspectionService.GetPagedAsync(query, cancellationToken));

    /// <summary>Карточка акта с результатами по параметрам и дефектами.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(QualityInspectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QualityInspectionDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _inspectionService.GetByIdAsync(id, cancellationToken));

    /// <summary>Оформление акта проверки.</summary>
    /// <remarks>
    /// Контролёр берётся из текущей учётной записи, а не из тела запроса.
    /// Акт, строки по параметрам, дефекты и новый статус партии сохраняются одной транзакцией.
    /// </remarks>
    /// <response code="403">Учётная запись не связана с сотрудником и не может подписывать акты.</response>
    /// <response code="409">Партия закрыта либо решение по ней уже принято.</response>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(QualityInspectionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QualityInspectionDto>> Create(
        CreateQualityInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var inspection = await _inspectionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = inspection.Id }, inspection);
    }

    /// <summary>Корректировка акта.</summary>
    /// <remarks>Доступна только подписавшему контролёру либо администратору.</remarks>
    /// <response code="403">Акт подписан другим контролёром.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(QualityInspectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<QualityInspectionDto>> Update(
        int id,
        UpdateQualityInspectionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _inspectionService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление акта.</summary>
    /// <response code="409">По дефектам акта созданы корректирующие действия.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _inspectionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
