using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Features.Users.Dtos;

public class RoleDto
{
    public int Id { get; init; }

    public string Name { get; init; } = null!;

    public string DisplayName { get; init; } = null!;

    public string? Description { get; init; }
}

public class UserDto
{
    public int Id { get; init; }

    public string UserName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public int RoleId { get; init; }

    public string RoleName { get; init; } = null!;

    public string RoleDisplayName { get; init; } = null!;

    public int? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public string? DepartmentName { get; init; }

    public bool IsActive { get; init; }

    public DateTime? LastLoginAt { get; init; }

    /// <summary>True while a lockout from failed sign-ins is still in force.</summary>
    public bool IsLockedOut { get; init; }

    public DateTime CreatedAt { get; init; }
}

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    int RoleId,
    int? EmployeeId);

public sealed record UpdateUserRequest(
    string Email,
    int RoleId,
    int? EmployeeId,
    bool IsActive);

/// <param name="NewPassword">Password the administrator is setting.</param>
public sealed record AdminResetPasswordRequest(string NewPassword);

public class UserQuery : PagedRequest
{
    /// <summary>Matches login name, email or the linked employee's surname.</summary>
    public string? Search { get; set; }

    public int? RoleId { get; set; }

    public bool? IsActive { get; set; }
}
