namespace QualityControl.Infrastructure.Services;

/// <summary>
/// Outgoing mail settings, bound from the "Email" configuration section.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// When false, messages are written to the application log instead of being sent.
    /// This is the default so that a developer with no SMTP server still sees the
    /// password-reset link — in the console — rather than a silent failure.
    /// </summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 25;

    public bool UseSsl { get; set; }

    public string? UserName { get; set; }

    /// <summary>Must come from a secret store, never from a committed settings file.</summary>
    public string? Password { get; set; }

    public string FromAddress { get; set; } = "otk@amkodor-pinsk.by";

    public string FromDisplayName { get; set; } = "АСКК «Амкодор-Пинск»";
}
