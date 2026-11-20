namespace QualityControl.Domain.Common;

/// <summary>
/// Entity that tracks who created and last modified it. The values are populated
/// centrally by the persistence layer on <c>SaveChanges</c>, never by hand.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
}
