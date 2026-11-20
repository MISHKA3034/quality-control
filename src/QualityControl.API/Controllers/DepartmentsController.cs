using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Departments;
using QualityControl.Application.Features.Departments.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Справочник подразделений (цеха, участки, отделы).
/// </summary>
[ApiController]
[Route("api/departments")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    /// <summary>Список подразделений с пагинацией.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DepartmentDto>>> GetPaged(
        [FromQuery] DepartmentQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _departmentService.GetPagedAsync(query, cancellationToken));

    /// <summary>Активные подразделения для выпадающих списков.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _departmentService.GetAllAsync(cancellationToken));

    /// <summary>Карточка подразделения.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _departmentService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание подразделения.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentDto>> Create(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    /// <summary>Изменение подразделения.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DepartmentDto>> Update(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _departmentService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление подразделения.</summary>
    /// <response code="409">В подразделении есть сотрудники, партии или вложенные подразделения.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _departmentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
