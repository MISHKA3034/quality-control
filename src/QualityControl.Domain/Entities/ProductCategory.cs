using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Product classifier — детали, металлоконструкции, узлы в сборе. Hierarchical.
/// </summary>
public class ProductCategory : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public ProductCategory? ParentCategory { get; set; }

    public ICollection<ProductCategory> ChildCategories { get; set; } = new List<ProductCategory>();

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
