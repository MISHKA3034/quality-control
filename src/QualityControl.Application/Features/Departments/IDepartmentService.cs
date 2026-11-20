using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Departments.Dtos;

namespace QualityControl.Application.Features.Departments;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentDto>> GetPagedAsync(
        DepartmentQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Active departments for dropdowns, ordered by name.</summary>
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DepartmentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);

    Task<DepartmentDto> UpdateAsync(
        int id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refused while the department still has employees, batches or child units.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
