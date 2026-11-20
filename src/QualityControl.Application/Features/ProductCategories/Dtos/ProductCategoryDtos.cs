using QualityControl.Application.Common.Models;

namespace QualityControl.Application.Features.ProductCategories.Dtos;

public class ProductCategoryDto
{
    public int Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public int? ParentCategoryId { get; init; }

    public string? ParentCategoryName { get; init; }

    public int ProductCount { get; init; }
}

public sealed record CreateProductCategoryRequest(
    string Code,
    string Name,
    string? Description,
    int? ParentCategoryId);

public sealed record UpdateProductCategoryRequest(
    string Code,
    string Name,
    string? Description,
    int? ParentCategoryId);

public class ProductCategoryQuery : PagedRequest
{
    /// <summary>Matches name or code.</summary>
    public string? Search { get; set; }

    public int? ParentCategoryId { get; set; }
}
