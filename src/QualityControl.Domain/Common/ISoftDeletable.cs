namespace QualityControl.Domain.Common;

/// <summary>
/// Marks an entity that is never physically removed. Quality records are evidence:
/// they must survive a "delete" so that historical inspections stay reconstructable.
/// A global query filter hides deleted rows from ordinary reads.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }

    int? DeletedByUserId { get; set; }
}
