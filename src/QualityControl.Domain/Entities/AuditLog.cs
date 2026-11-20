using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Immutable record of a user action. Append-only — the application never updates
/// or deletes these rows, which is what makes the log usable as evidence.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>Null for actions with no established identity, e.g. a failed sign-in.</summary>
    public int? UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Login name captured at the time of the action. Denormalised on purpose: the
    /// log must stay readable even if the account is later renamed or removed.
    /// </summary>
    public string? UserName { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Type the action touched, e.g. "Product". Null for non-entity actions like sign-in.</summary>
    public string? EntityName { get; set; }

    public string? EntityId { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// JSON diff of the changed properties (old/new). Populated for updates; null otherwise.
    /// </summary>
    public string? Changes { get; set; }

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>False when the attempted action was rejected — a failed sign-in, for instance.</summary>
    public bool IsSuccess { get; set; } = true;
}
