using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Employees.Dtos;

namespace QualityControl.Application.Features.Employees;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetPagedAsync(EmployeeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active employees for dropdowns, optionally narrowed to one department.
    /// </summary>
    Task<IReadOnlyList<EmployeeDto>> GetAllActiveAsync(
        int? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<EmployeeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeDto> UpdateAsync(
        int id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refused once the employee appears on any quality document.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
