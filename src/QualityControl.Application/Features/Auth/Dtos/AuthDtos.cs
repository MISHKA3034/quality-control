namespace QualityControl.Application.Features.Auth.Dtos;

/// <param name="UserName">Login name or email — both are accepted.</param>
/// <param name="Password">Plain-text password; never logged.</param>
public sealed record LoginRequest(string UserName, string Password);

/// <param name="RefreshToken">The raw refresh token previously issued to this client.</param>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <param name="Email">Address to send the reset link to.</param>
public sealed record ForgotPasswordRequest(string Email);

/// <param name="Email">Address the reset was requested for.</param>
/// <param name="Token">Raw single-use token from the emailed link.</param>
/// <param name="NewPassword">Replacement password.</param>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

/// <param name="CurrentPassword">Existing password, re-checked to prove the session is not hijacked.</param>
/// <param name="NewPassword">Replacement password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// The signed-in user, as the SPA needs them: identity plus what drives the menu.
/// </summary>
public sealed class CurrentUserDto
{
    public int Id { get; init; }

    public string UserName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string Role { get; init; } = null!;

    public string RoleDisplayName { get; init; } = null!;

    public int? EmployeeId { get; init; }

    public string? FullName { get; init; }

    public string? Position { get; init; }

    public int? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }
}

/// <summary>
/// Result of a successful sign-in or refresh.
/// </summary>
public sealed class AuthResponse
{
    public string AccessToken { get; init; } = null!;

    public DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// Raw refresh token. Rotated on every use — the value returned here replaces
    /// whatever the client held before.
    /// </summary>
    public string RefreshToken { get; init; } = null!;

    public DateTime RefreshTokenExpiresAt { get; init; }

    public CurrentUserDto User { get; init; } = null!;
}
