using QualityControl.Application.Common.Models;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.ProductionBatches.Dtos;

public class ProductionBatchListItemDto
{
    public int Id { get; init; }

    public string BatchNumber { get; init; } = null!;

    public int ProductId { get; init; }

    public string ProductName { get; init; } = null!;

    public string ProductCode { get; init; } = null!;

    public DateTime ManufactureDate { get; init; }

    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public int ForemanId { get; init; }

    public string ForemanName { get; init; } = null!;

    public int Quantity { get; init; }

    public BatchStatus Status { get; init; }

    public int InspectionCount { get; init; }
}

public class ProductionBatchDto : ProductionBatchListItemDto
{
    public string? Notes { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Statuses this batch may move to right now, so the UI can render only the
    /// transitions the domain will actually accept.
    /// </summary>
    public IReadOnlyList<BatchStatus> AllowedNextStatuses { get; set; } = [];
}

public sealed record CreateProductionBatchRequest(
    string BatchNumber,
    int ProductId,
    DateTime ManufactureDate,
    int DepartmentId,
    int ForemanId,
    int Quantity,
    string? Notes);

public sealed record UpdateProductionBatchRequest(
    string BatchNumber,
    int ProductId,
    DateTime ManufactureDate,
    int DepartmentId,
    int ForemanId,
    int Quantity,
    string? Notes);

/// <param name="Status">Target status.</param>
/// <param name="Comment">Optional note recorded in the audit trail.</param>
public sealed record ChangeBatchStatusRequest(BatchStatus Status, string? Comment);

public class ProductionBatchQuery : PagedRequest
{
    /// <summary>Matches batch number, product name or product code.</summary>
    public string? Search { get; set; }

    public int? ProductId { get; set; }

    public int? DepartmentId { get; set; }

    public int? ForemanId { get; set; }

    public BatchStatus? Status { get; set; }

    /// <summary>Inclusive lower bound on manufacture date.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Inclusive upper bound on manufacture date.</summary>
    public DateTime? DateTo { get; set; }
}
