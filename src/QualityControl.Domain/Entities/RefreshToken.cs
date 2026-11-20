using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// A refresh token issued to one user. Tokens are stored hashed, rotated on every
/// use, and revoked in a chain so that replay of a consumed token can be detected.
/// </summary>
public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 of the token that was handed to the client. The raw value is never stored.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Hash of the token that superseded this one. Lets a replayed token be traced
    /// to its descendants so the whole chain can be revoked.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    public string? RevocationReason { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);
}
