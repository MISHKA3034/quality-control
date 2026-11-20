using System.ComponentModel.DataAnnotations;

namespace QualityControl.Infrastructure.Security;

/// <summary>
/// JWT signing settings, bound from the "Jwt" configuration section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Symmetric signing key. Must come from a secret store or environment variable
    /// in production — never from a committed appsettings file.
    /// </summary>
    [Required(ErrorMessage = "Не задан ключ подписи JWT (Jwt:Key).")]
    [MinLength(32, ErrorMessage = "Ключ подписи JWT должен быть не короче 32 символов.")]
    public string Key { get; set; } = null!;

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;
}
