using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Classifier of defects — the plant's справочник видов дефектов (несоответствие
/// размеров, непровар шва, дефект покрытия, …). Used for the "частые дефекты" report.
/// </summary>
public class DefectType : AuditableEntity, ISoftDeletable
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Pre-selected severity when an inspector picks this type; still overridable.</summary>
    public DefectSeverity DefaultSeverity { get; set; } = DefectSeverity.Major;

    /// <summary>Inactive types stay on historical defects but are hidden from new registrations.</summary>
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<Defect> Defects { get; set; } = new List<Defect>();
}
