using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Products.Dtos;

namespace QualityControl.Application.Features.Products;

/// <summary>
/// Product catalogue use cases.
/// </summary>
public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a product. Refused once the product has production batches —
    /// archive it instead, so its history stays intact.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Withdraws a product from use without touching its history.</summary>
    Task<ProductDto> ArchiveAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductDto> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
