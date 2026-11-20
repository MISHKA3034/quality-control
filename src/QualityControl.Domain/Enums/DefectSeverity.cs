namespace QualityControl.Domain.Enums;

/// <summary>
/// Criticality of a registered defect.
/// </summary>
public enum DefectSeverity
{
    /// <summary>Незначительный — не влияет на работоспособность.</summary>
    Minor = 1,

    /// <summary>Значительный — влияет на характеристики изделия.</summary>
    Major = 2,

    /// <summary>Критический — эксплуатация недопустима.</summary>
    Critical = 3
}
