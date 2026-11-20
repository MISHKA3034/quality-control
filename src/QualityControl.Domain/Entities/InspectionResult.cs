using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;

namespace QualityControl.Domain.Entities;

/// <summary>
/// One checked characteristic within an inspection — a measured parameter with its
/// required and actual value. An inspection act normally lists several of these.
/// </summary>
public class InspectionResult : BaseEntity
{
    public int QualityInspectionId { get; set; }

    public QualityInspection QualityInspection { get; set; } = null!;

    /// <summary>Контролируемый параметр, e.g. "Диаметр отверстия Ø20H7".</summary>
    public string ParameterName { get; set; } = null!;

    /// <summary>Требование по чертежу/ТУ, e.g. "20,0…20,021 мм".</summary>
    public string? ExpectedValue { get; set; }

    /// <summary>Фактическое значение, e.g. "20,015 мм".</summary>
    public string? ActualValue { get; set; }

    /// <summary>Единица измерения параметра.</summary>
    public string? UnitOfMeasure { get; set; }

    public InspectionOutcome Outcome { get; set; }

    public string? Comment { get; set; }

    /// <summary>Порядок строки в акте.</summary>
    public int SortOrder { get; set; }
}
