using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Single-use token backing the "forgot password" flow. Stored hashed and
/// short-lived; consuming it marks <see cref="UsedAt"/> so it cannot be replayed.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 of the token emailed to the user. The raw value is never stored.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? RequestedByIp { get; set; }

    public DateTime? UsedAt { get; set; }

    public bool IsUsed => UsedAt is not null;

    public bool IsActive(DateTime utcNow) => !IsUsed && utcNow < ExpiresAt;
}
