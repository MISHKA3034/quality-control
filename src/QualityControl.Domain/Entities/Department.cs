using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Organisational unit — цех, участок or отдел. Self-referencing so that a участок
/// can sit under its цех and reports can roll up the hierarchy.
/// </summary>
public class Department : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    /// <summary>Short code used on shop-floor documents, e.g. "ЦМК-1".</summary>
    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentDepartmentId { get; set; }

    public Department? ParentDepartment { get; set; }

    public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();

    /// <summary>Начальник подразделения. Nullable so a unit can exist between appointments.</summary>
    public int? ManagerId { get; set; }

    public Employee? Manager { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();
}
