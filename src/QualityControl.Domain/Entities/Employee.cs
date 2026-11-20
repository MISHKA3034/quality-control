using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Personnel record. Referenced by every document that needs a signature —
/// inspections, corrective actions, batch foremen.
/// </summary>
public class Employee : AuditableEntity, ISoftDeletable
{
    /// <summary>Табельный номер — unique across the plant.</summary>
    public string PersonnelNumber { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string Position { get; set; } = null!;

    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? HiredOn { get; set; }

    public DateTime? DismissedOn { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    /// <summary>Фамилия И.О. — the form used on documents and in dropdowns.</summary>
    public string ShortName =>
        $"{LastName} {FirstName[..1]}.{(string.IsNullOrEmpty(MiddleName) ? string.Empty : $"{MiddleName[..1]}.")}";

    public string FullName =>
        string.Join(' ', new[] { LastName, FirstName, MiddleName }.Where(p => !string.IsNullOrWhiteSpace(p)));

    public User? User { get; set; }

    public ICollection<ProductionBatch> SupervisedBatches { get; set; } = new List<ProductionBatch>();

    public ICollection<QualityInspection> Inspections { get; set; } = new List<QualityInspection>();

    public ICollection<CorrectiveAction> AssignedCorrectiveActions { get; set; } = new List<CorrectiveAction>();
}
