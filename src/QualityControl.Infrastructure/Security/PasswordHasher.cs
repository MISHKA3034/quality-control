using Microsoft.Extensions.Logging;
using QualityControl.Application.Common.Interfaces;

namespace QualityControl.Infrastructure.Security;

/// <summary>
/// BCrypt-based password hashing.
/// </summary>
/// <remarks>
/// BCrypt embeds its salt and cost factor in the hash string, so an existing hash
/// keeps verifying after <see cref="WorkFactor"/> is raised. Increasing the factor
/// therefore only affects newly set passwords, and needs no migration.
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Cost factor. 12 is roughly 250 ms per hash on current hardware — slow enough
    /// to make offline cracking expensive, fast enough for an interactive sign-in.
    /// </summary>
    private const int WorkFactor = 12;

    private readonly ILogger<PasswordHasher> _logger;

    public PasswordHasher(ILogger<PasswordHasher> logger)
    {
        _logger = logger;
    }

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored hash that BCrypt cannot parse is corrupt or was written by
            // another scheme. Failing the check is right; throwing would turn a bad
            // row into a 500 and leak that this account is special.
            _logger.LogError("Не удалось разобрать сохранённый хеш пароля. Проверка отклонена.");
            return false;
        }
    }
}
