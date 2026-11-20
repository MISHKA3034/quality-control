using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;

namespace QualityControl.Domain.Entities;

/// <summary>
/// A defect found during an inspection.
/// </summary>
public class Defect : AuditableEntity, ISoftDeletable
{
    public int QualityInspectionId { get; set; }

    public QualityInspection QualityInspection { get; set; } = null!;

    public int DefectTypeId { get; set; }

    public DefectType DefectType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DefectSeverity Severity { get; set; }

    /// <summary>Место возникновения — узел, шов, поверхность.</summary>
    public string? Location { get; set; }

    public string? Comment { get; set; }

    /// <summary>Число изделий партии с этим дефектом.</summary>
    public int AffectedQuantity { get; set; } = 1;

    public DateTime DetectedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<DefectPhoto> Photos { get; set; } = new List<DefectPhoto>();

    public ICollection<CorrectiveAction> CorrectiveActions { get; set; } = new List<CorrectiveAction>();

    /// <summary>
    /// A defect counts as open until every corrective action raised for it is closed.
    /// A defect with no corrective actions yet is open by definition.
    /// </summary>
    public bool IsOpen =>
        CorrectiveActions.Count == 0
        || CorrectiveActions.Any(a => a.Status != CorrectiveActionStatus.Closed);
}
