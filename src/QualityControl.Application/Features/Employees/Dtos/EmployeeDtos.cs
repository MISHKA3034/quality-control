using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Features.Employees.Dtos;

public class EmployeeDto
{
    public int Id { get; init; }

    public string PersonnelNumber { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string? MiddleName { get; init; }

    public string FullName { get; init; } = null!;

    public string ShortName { get; init; } = null!;

    public string Position { get; init; } = null!;

    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public DateTime? HiredOn { get; init; }

    public DateTime? DismissedOn { get; init; }

    public bool IsActive { get; init; }

    /// <summary>True when the employee has a login account.</summary>
    public bool HasUserAccount { get; init; }
}

public sealed record CreateEmployeeRequest(
    string PersonnelNumber,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Position,
    int DepartmentId,
    string? Email,
    string? Phone,
    DateTime? HiredOn);

public sealed record UpdateEmployeeRequest(
    string PersonnelNumber,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Position,
    int DepartmentId,
    string? Email,
    string? Phone,
    DateTime? HiredOn,
    DateTime? DismissedOn,
    bool IsActive);

public class EmployeeQuery : PagedRequest
{
    /// <summary>Matches surname, name, personnel number or position.</summary>
    public string? Search { get; set; }

    public int? DepartmentId { get; set; }

    public bool? IsActive { get; set; }
}
