namespace QualityControl.Domain.Enums;

/// <summary>
/// Verdict of a quality inspection, both for the inspection as a whole and for
/// each checked characteristic.
/// </summary>
public enum InspectionOutcome
{
    /// <summary>Соответствует требованиям.</summary>
    Conforming = 1,

    /// <summary>Требуется доработка.</summary>
    ReworkRequired = 2,

    /// <summary>Брак.</summary>
    Defective = 3
}
