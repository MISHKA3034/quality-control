using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// A login account. Personal data lives on the linked <see cref="Employee"/>;
/// this entity holds only what authentication needs.
/// </summary>
public class User : AuditableEntity, ISoftDeletable
{
    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    /// <summary>
    /// The personnel record this account acts as. Required for anyone who signs
    /// inspections or is assigned corrective actions; optional for pure
    /// administrator accounts that never appear on a quality document.
    /// </summary>
    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Consecutive failed sign-in attempts, reset on success. Drives <see cref="LockoutEndsAt"/>.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// When set and in the future, authentication is refused regardless of password.
    /// </summary>
    public DateTime? LockoutEndsAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
