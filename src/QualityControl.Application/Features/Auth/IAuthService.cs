using QualityControl.Application.Features.Auth.Dtos;

namespace QualityControl.Application.Features.Auth;

/// <summary>
/// Sign-in, session renewal and password lifecycle.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and starts a session.
    /// </summary>
    /// <exception cref="Common.Exceptions.ValidationException">
    /// Credentials are wrong, the account is disabled, or it is locked out. The
    /// message never distinguishes these cases to a caller.
    /// </exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a new pair, rotating the refresh token.
    /// </summary>
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>Revokes the supplied refresh token, ending that session.</summary>
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the password-reset flow. Completes silently whether or not the address
    /// is registered, so the endpoint cannot be used to enumerate accounts.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Completes a password reset and revokes every existing session.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Changes the signed-in user's password and revokes their other sessions.</summary>
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Profile of the signed-in user.</summary>
    Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
