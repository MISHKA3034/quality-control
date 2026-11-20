namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// The identity behind the current request. Implemented in the API layer over
/// <c>HttpContext</c>; keeps the Application layer free of ASP.NET Core types.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Null for unauthenticated requests and for background work.</summary>
    int? UserId { get; }

    string? UserName { get; }

    /// <summary>Personnel record of the signed-in account, when it has one.</summary>
    int? EmployeeId { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }

    bool IsInRole(string role);
}
