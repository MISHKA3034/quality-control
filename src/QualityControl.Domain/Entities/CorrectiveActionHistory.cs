using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;

namespace QualityControl.Domain.Entities;

/// <summary>
/// One entry in the change history of a corrective action. Append-only: rows are
/// written by <see cref="CorrectiveAction.ChangeStatus"/> and never updated.
/// </summary>
public class CorrectiveActionHistory : BaseEntity
{
    public int CorrectiveActionId { get; set; }

    public CorrectiveAction CorrectiveAction { get; set; } = null!;

    /// <summary>Null for the entry that records creation.</summary>
    public CorrectiveActionStatus? FromStatus { get; set; }

    public CorrectiveActionStatus ToStatus { get; set; }

    public int ChangedByUserId { get; set; }

    public User ChangedByUser { get; set; } = null!;

    public DateTime ChangedAt { get; set; }

    public string? Comment { get; set; }
}
