using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.DefectTypes;
using QualityControl.Application.Features.DefectTypes.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Справочник видов дефектов.
/// </summary>
[ApiController]
[Route("api/defect-types")]
[Authorize]
[Produces("application/json")]
public class DefectTypesController : ControllerBase
{
    private readonly IDefectTypeService _defectTypeService;

    public DefectTypesController(IDefectTypeService defectTypeService)
    {
        _defectTypeService = defectTypeService;
    }

    /// <summary>Список видов дефектов с пагинацией.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DefectTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DefectTypeDto>>> GetPaged(
        [FromQuery] DefectTypeQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _defectTypeService.GetPagedAsync(query, cancellationToken));

    /// <summary>Активные виды дефектов для выпадающих списков.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<DefectTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DefectTypeDto>>> GetAllActive(
        CancellationToken cancellationToken) =>
        Ok(await _defectTypeService.GetAllActiveAsync(cancellationToken));

    /// <summary>Карточка вида дефекта.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DefectTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DefectTypeDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _defectTypeService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание вида дефекта.</summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(DefectTypeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DefectTypeDto>> Create(
        CreateDefectTypeRequest request,
        CancellationToken cancellationToken)
    {
        var defectType = await _defectTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = defectType.Id }, defectType);
    }

    /// <summary>Изменение вида дефекта.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{RoleNames.Administrator},{RoleNames.QualityInspector}")]
    [ProducesResponseType(typeof(DefectTypeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DefectTypeDto>> Update(
        int id,
        UpdateDefectTypeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _defectTypeService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление вида дефекта.</summary>
    /// <response code="409">Вид дефекта используется — снимите отметку «Активен».</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _defectTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
