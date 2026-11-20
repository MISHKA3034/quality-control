using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductCategories.Dtos;

namespace QualityControl.Application.Features.ProductCategories;

public interface IProductCategoryService
{
    Task<PagedResult<ProductCategoryDto>> GetPagedAsync(
        ProductCategoryQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Flat list for dropdowns, ordered by name.</summary>
    Task<IReadOnlyList<ProductCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductCategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductCategoryDto> CreateAsync(
        CreateProductCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductCategoryDto> UpdateAsync(
        int id,
        UpdateProductCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refused while the category still holds products or child categories.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
