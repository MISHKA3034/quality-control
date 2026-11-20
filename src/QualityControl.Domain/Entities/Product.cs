using QualityControl.Domain.Common;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Domain.Entities;

/// <summary>
/// A manufactured item — деталь, металлоконструкция or собранный узел.
/// </summary>
public class Product : AuditableEntity, ISoftDeletable
{
    /// <summary>Артикул/обозначение — unique across the catalogue.</summary>
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public ProductCategory Category { get; set; } = null!;

    /// <summary>Обозначение чертежа, e.g. "АМ-3416.02.05.100".</summary>
    public string? DrawingNumber { get; set; }

    /// <summary>Единица измерения — шт., кг, м.</summary>
    public string UnitOfMeasure { get; set; } = "шт.";

    /// <summary>
    /// Archived products stay fully readable and keep their history, but cannot be
    /// selected for new production batches.
    /// </summary>
    public bool IsArchived { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public int? ArchivedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<ProductionBatch> ProductionBatches { get; set; } = new List<ProductionBatch>();

    public void Archive(DateTime utcNow, int? byUserId)
    {
        if (IsArchived)
        {
            throw new DomainException($"Изделие «{Name}» уже находится в архиве.");
        }

        IsArchived = true;
        ArchivedAt = utcNow;
        ArchivedByUserId = byUserId;
    }

    public void Restore()
    {
        if (!IsArchived)
        {
            throw new DomainException($"Изделие «{Name}» не находится в архиве.");
        }

        IsArchived = false;
        ArchivedAt = null;
        ArchivedByUserId = null;
    }
}
