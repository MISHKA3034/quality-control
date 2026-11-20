using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.ProductionBatches.Dtos;

namespace QualityControl.Application.Features.ProductionBatches;

public interface IProductionBatchService
{
    Task<PagedResult<ProductionBatchListItemDto>> GetPagedAsync(
        ProductionBatchQuery query,
        CancellationToken cancellationToken = default);

    Task<ProductionBatchDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batches an inspector may file an act against — everything not yet closed.
    /// </summary>
    Task<IReadOnlyList<ProductionBatchListItemDto>> GetInspectableAsync(
        CancellationToken cancellationToken = default);

    Task<ProductionBatchDto> CreateAsync(
        CreateProductionBatchRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductionBatchDto> UpdateAsync(
        int id,
        UpdateProductionBatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Moves the batch through its status machine; illegal transitions are refused.</summary>
    Task<ProductionBatchDto> ChangeStatusAsync(
        int id,
        ChangeBatchStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refused once the batch has inspection acts.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
