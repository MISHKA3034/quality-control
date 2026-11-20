using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Domain.Entities;

/// <summary>
/// One quality inspection of a production batch, performed by an OTK inspector.
/// Carries the overall verdict; the per-characteristic detail lives in
/// <see cref="Results"/> and any findings in <see cref="Defects"/>.
/// </summary>
public class QualityInspection : AuditableEntity, ISoftDeletable
{
    /// <summary>Номер акта проверки.</summary>
    public string InspectionNumber { get; set; } = null!;

    public int ProductionBatchId { get; set; }

    public ProductionBatch ProductionBatch { get; set; } = null!;

    /// <summary>Контролёр ОТК, подписавший акт.</summary>
    public int InspectorId { get; set; }

    public Employee Inspector { get; set; } = null!;

    public DateTime InspectionDate { get; set; }

    public InspectionOutcome Outcome { get; set; }

    public string? Comment { get; set; }

    /// <summary>Проверено единиц продукции (выборка или вся партия).</summary>
    public int QuantityInspected { get; set; }

    /// <summary>Принято единиц.</summary>
    public int QuantityAccepted { get; set; }

    /// <summary>Забраковано единиц.</summary>
    public int QuantityRejected { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<InspectionResult> Results { get; set; } = new List<InspectionResult>();

    public ICollection<Defect> Defects { get; set; } = new List<Defect>();

    /// <summary>
    /// The inspected quantities must add up and must not exceed the batch size.
    /// Enforced here so no caller can persist an internally inconsistent act.
    /// </summary>
    public void ValidateQuantities(int batchQuantity)
    {
        if (QuantityInspected <= 0)
        {
            throw new DomainException("Количество проверенных изделий должно быть больше нуля.");
        }

        if (QuantityInspected > batchQuantity)
        {
            throw new DomainException(
                $"Проверено изделий ({QuantityInspected}) больше, чем в партии ({batchQuantity}).");
        }

        if (QuantityAccepted + QuantityRejected != QuantityInspected)
        {
            throw new DomainException(
                $"Сумма принятых ({QuantityAccepted}) и забракованных ({QuantityRejected}) изделий "
                + $"не равна количеству проверенных ({QuantityInspected}).");
        }
    }

    /// <summary>
    /// Доля брака в выборке — the basis of the defect-rate figures on the dashboard.
    /// </summary>
    public decimal DefectRate =>
        QuantityInspected == 0 ? 0m : (decimal)QuantityRejected / QuantityInspected;
}
