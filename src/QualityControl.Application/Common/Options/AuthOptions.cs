namespace QualityControl.Application.Common.Options;

/// <summary>
/// Authentication policy, bound from the "Auth" configuration section.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Access-token lifetime. Deliberately short: a leaked access token cannot be
    /// revoked, so it must expire quickly. Renewal goes through the refresh token.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>Refresh-token lifetime — how long a session can survive without re-entering a password.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 7;

    public int PasswordResetTokenLifetimeMinutes { get; set; } = 30;

    /// <summary>Failed sign-ins before the account locks. Blunts online password guessing.</summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int MinPasswordLength { get; set; } = 8;

    /// <summary>
    /// Base address of the SPA, used to build the link in the password-reset email.
    /// </summary>
    public string ClientBaseUrl { get; set; } = "http://localhost:5173";
}
