using QualityControl.Application.Common.Models;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.Defects.Dtos;

public class DefectPhotoDto
{
    public int Id { get; init; }

    public int DefectId { get; init; }

    public string OriginalFileName { get; init; } = null!;

    public string StoredFileName { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public long FileSizeBytes { get; init; }

    public string? Caption { get; init; }

    public DateTime UploadedAt { get; init; }

    /// <summary>Relative API path the client uses to fetch the image.</summary>
    public string Url => $"/api/defects/{DefectId}/photos/{Id}/content";
}

public class DefectListItemDto
{
    public int Id { get; init; }

    public int QualityInspectionId { get; init; }

    public string InspectionNumber { get; init; } = null!;

    public int ProductionBatchId { get; init; }

    public string BatchNumber { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public int DefectTypeId { get; init; }

    public string DefectTypeName { get; init; } = null!;

    public string Description { get; init; } = null!;

    public DefectSeverity Severity { get; init; }

    public string? Location { get; init; }

    public int AffectedQuantity { get; init; }

    public DateTime DetectedAt { get; init; }

    public int PhotoCount { get; init; }

    /// <summary>Open until every corrective action raised for it is closed.</summary>
    public bool IsOpen { get; init; }

    public int CorrectiveActionCount { get; init; }
}

public class DefectDto : DefectListItemDto
{
    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<DefectPhotoDto> Photos { get; init; } = [];
}

public sealed record CreateDefectRequest(
    int QualityInspectionId,
    int DefectTypeId,
    string Description,
    DefectSeverity Severity,
    string? Location,
    string? Comment,
    int AffectedQuantity);

public sealed record UpdateDefectRequest(
    int DefectTypeId,
    string Description,
    DefectSeverity Severity,
    string? Location,
    string? Comment,
    int AffectedQuantity);

public class DefectQuery : PagedRequest
{
    /// <summary>Matches description, location, batch number or product name.</summary>
    public string? Search { get; set; }

    public int? QualityInspectionId { get; set; }

    public int? ProductionBatchId { get; set; }

    public int? ProductId { get; set; }

    public int? DefectTypeId { get; set; }

    public int? DepartmentId { get; set; }

    public int? InspectorId { get; set; }

    public DefectSeverity? Severity { get; set; }

    /// <summary>True returns defects with unfinished corrective actions.</summary>
    public bool? IsOpen { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
