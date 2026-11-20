using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Employees;
using QualityControl.Application.Features.Employees.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Справочник сотрудников.
/// </summary>
[ApiController]
[Route("api/employees")]
[Authorize]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>Список сотрудников с поиском и пагинацией.</summary>
    /// <remarks>Поиск идёт по фамилии, имени, табельному номеру и должности.</remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EmployeeDto>>> GetPaged(
        [FromQuery] EmployeeQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _employeeService.GetPagedAsync(query, cancellationToken));

    /// <summary>Активные сотрудники для выпадающих списков.</summary>
    /// <param name="departmentId">Необязательный фильтр по подразделению.</param>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> GetAllActive(
        [FromQuery] int? departmentId,
        CancellationToken cancellationToken) =>
        Ok(await _employeeService.GetAllActiveAsync(departmentId, cancellationToken));

    /// <summary>Карточка сотрудника.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _employeeService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание сотрудника.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<EmployeeDto>> Create(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var employee = await _employeeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    /// <summary>Изменение сотрудника.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmployeeDto>> Update(
        int id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _employeeService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Удаление сотрудника.</summary>
    /// <response code="409">Сотрудник указан в документах качества — отметьте его как уволенного.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _employeeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
