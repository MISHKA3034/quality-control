using QualityControl.Domain.Entities;

namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Issues access tokens and the opaque secrets behind refresh/password-reset flows.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Builds a signed JWT carrying the user's id, name, role and employee id.
    /// </summary>
    /// <returns>The compact token and the moment it expires.</returns>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random token. Returns the raw value to hand to
    /// the client and the hash to store — the raw value is never persisted.
    /// </summary>
    (string Token, string TokenHash) CreateSecureToken();

    /// <summary>Hashes a raw token so it can be looked up against stored hashes.</summary>
    string HashToken(string token);
}
