using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Options;
using QualityControl.Domain.Entities;

namespace QualityControl.Infrastructure.Security;

/// <inheritdoc cref="ITokenService"/>
public class TokenService : ITokenService
{
    /// <summary>Entropy of refresh and reset tokens. 256 bits is not guessable.</summary>
    private const int TokenSizeBytes = 32;

    /// <summary>Claim carrying the signed-in user's personnel record id.</summary>
    public const string EmployeeIdClaim = "employee_id";

    private readonly JwtOptions _jwt;
    private readonly AuthOptions _auth;
    private readonly IDateTimeProvider _clock;

    public TokenService(
        IOptions<JwtOptions> jwt,
        IOptions<AuthOptions> auth,
        IDateTimeProvider clock)
    {
        _jwt = jwt.Value;
        _auth = auth.Value;
        _clock = clock;
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_auth.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),

            // Distinguishes one issued token from another, so a specific token can be
            // referenced in the logs.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.Name)
        };

        if (user.EmployeeId is not null)
        {
            claims.Add(new Claim(EmployeeIdClaim, user.EmployeeId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string Token, string TokenHash) CreateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSizeBytes);

        // Base64Url: the value travels in URLs (the reset link) and JSON, and must
        // survive both without escaping.
        var token = Base64UrlEncoder.Encode(bytes);

        return (token, HashToken(token));
    }

    /// <summary>
    /// Hashes a token for storage and lookup.
    /// </summary>
    /// <remarks>
    /// Plain SHA-256, not BCrypt. These tokens are 256 bits of cryptographic
    /// randomness, so there is no low-entropy secret to protect against brute force —
    /// the reason a password needs a slow hash does not apply. A fast digest also
    /// keeps the lookup indexable, which a per-row salted hash would not be.
    /// </remarks>
    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
