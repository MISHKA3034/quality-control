using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Features.Departments.Dtos;

public class DepartmentDto
{
    public int Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public int? ParentDepartmentId { get; init; }

    public string? ParentDepartmentName { get; init; }

    public int? ManagerId { get; init; }

    public string? ManagerName { get; init; }

    public bool IsActive { get; init; }

    public int EmployeeCount { get; init; }
}

public sealed record CreateDepartmentRequest(
    string Code,
    string Name,
    string? Description,
    int? ParentDepartmentId,
    int? ManagerId);

public sealed record UpdateDepartmentRequest(
    string Code,
    string Name,
    string? Description,
    int? ParentDepartmentId,
    int? ManagerId,
    bool IsActive);

public class DepartmentQuery : PagedRequest
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public int? ParentDepartmentId { get; set; }
}
