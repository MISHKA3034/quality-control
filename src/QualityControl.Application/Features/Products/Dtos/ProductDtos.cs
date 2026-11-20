using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Features.Products.Dtos;

/// <summary>Row shown in the product list.</summary>
public class ProductListItemDto
{
    public int Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public int CategoryId { get; init; }

    public string CategoryName { get; init; } = null!;

    public string? DrawingNumber { get; init; }

    public string UnitOfMeasure { get; init; } = null!;

    public bool IsArchived { get; init; }

    public int BatchCount { get; init; }
}

/// <summary>Full product card.</summary>
public class ProductDto : ProductListItemDto
{
    public string? Description { get; init; }

    public DateTime? ArchivedAt { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}

public sealed record CreateProductRequest(
    string Code,
    string Name,
    int CategoryId,
    string? Description,
    string? DrawingNumber,
    string UnitOfMeasure);

public sealed record UpdateProductRequest(
    string Code,
    string Name,
    int CategoryId,
    string? Description,
    string? DrawingNumber,
    string UnitOfMeasure);

/// <summary>
/// Filters for the product list. All are optional and combine with AND.
/// </summary>
public class ProductQuery : PagedRequest
{
    /// <summary>Matches name, code or drawing number.</summary>
    public string? Search { get; set; }

    public int? CategoryId { get; set; }

    /// <summary>
    /// Null returns only active products. Archived items are excluded by default so
    /// that day-to-day screens are not cluttered with withdrawn drawings.
    /// </summary>
    public bool? IsArchived { get; set; }
}
