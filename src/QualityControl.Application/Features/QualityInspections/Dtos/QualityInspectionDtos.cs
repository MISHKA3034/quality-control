using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.QualityInspections.Dtos;

public class InspectionResultDto
{
    public int Id { get; init; }

    public string ParameterName { get; init; } = null!;

    public string? ExpectedValue { get; init; }

    public string? ActualValue { get; init; }

    public string? UnitOfMeasure { get; init; }

    public InspectionOutcome Outcome { get; init; }

    public string? Comment { get; init; }

    public int SortOrder { get; init; }
}

public class QualityInspectionListItemDto
{
    public int Id { get; init; }

    public string InspectionNumber { get; init; } = null!;

    public int ProductionBatchId { get; init; }

    public string BatchNumber { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public int InspectorId { get; init; }

    public string InspectorName { get; init; } = null!;

    public DateTime InspectionDate { get; init; }

    public InspectionOutcome Outcome { get; init; }

    public int QuantityInspected { get; init; }

    public int QuantityAccepted { get; init; }

    public int QuantityRejected { get; init; }

    public int DefectCount { get; init; }
}

public class QualityInspectionDto : QualityInspectionListItemDto
{
    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<InspectionResultDto> Results { get; init; } = [];

    public IReadOnlyList<DefectListItemDto> Defects { get; init; } = [];
}

/// <param name="ParameterName">Контролируемый параметр.</param>
/// <param name="Outcome">Вердикт по этому параметру.</param>
public sealed record CreateInspectionResultRequest(
    string ParameterName,
    string? ExpectedValue,
    string? ActualValue,
    string? UnitOfMeasure,
    InspectionOutcome Outcome,
    string? Comment,
    int SortOrder);

/// <summary>
/// Defect registered as part of filing the act. Photographs are attached afterwards
/// through the defect endpoints, since they arrive as multipart uploads.
/// </summary>
public sealed record CreateInspectionDefectRequest(
    int DefectTypeId,
    string Description,
    DefectSeverity Severity,
    string? Location,
    string? Comment,
    int AffectedQuantity);

/// <summary>
/// An inspection act. The inspector is taken from the signed-in account rather than
/// the payload — an act must be signed by whoever actually filed it.
/// </summary>
public sealed record CreateQualityInspectionRequest(
    int ProductionBatchId,
    DateTime InspectionDate,
    InspectionOutcome Outcome,
    string? Comment,
    int QuantityInspected,
    int QuantityAccepted,
    int QuantityRejected,
    IReadOnlyList<CreateInspectionResultRequest> Results,
    IReadOnlyList<CreateInspectionDefectRequest> Defects);

/// <summary>
/// Editable fields of a filed act. The batch, the inspector and the verdict are not
/// among them: changing those would rewrite a decision that has already had effects.
/// </summary>
public sealed record UpdateQualityInspectionRequest(
    DateTime InspectionDate,
    string? Comment,
    IReadOnlyList<CreateInspectionResultRequest> Results);

public class QualityInspectionQuery : PagedRequest
{
    /// <summary>Matches act number, batch number or product name.</summary>
    public string? Search { get; set; }

    public int? ProductionBatchId { get; set; }

    public int? ProductId { get; set; }

    public int? InspectorId { get; set; }

    public int? DepartmentId { get; set; }

    public InspectionOutcome? Outcome { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    /// <summary>Narrows to acts that found at least one defect of this type.</summary>
    public int? DefectTypeId { get; set; }
}
