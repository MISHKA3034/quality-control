using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Domain.Entities;

/// <summary>
/// A production batch — the unit that OTK actually inspects.
/// </summary>
public class ProductionBatch : AuditableEntity, ISoftDeletable
{
    /// <summary>Номер партии, unique across the plant.</summary>
    public string BatchNumber { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public DateTime ManufactureDate { get; set; }

    /// <summary>Участок изготовления.</summary>
    public int DepartmentId { get; set; }

    public Department Department { get; set; } = null!;

    /// <summary>Ответственный мастер.</summary>
    public int ForemanId { get; set; }

    public Employee Foreman { get; set; } = null!;

    /// <summary>Количество изделий в партии.</summary>
    public int Quantity { get; set; }

    public BatchStatus Status { get; set; } = BatchStatus.InProduction;

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<QualityInspection> Inspections { get; set; } = new List<QualityInspection>();

    /// <summary>
    /// Transitions allowed by the OTK process. Anything not listed is rejected —
    /// a batch cannot, for example, jump from InProduction straight to Accepted
    /// without being inspected.
    /// </summary>
    private static readonly Dictionary<BatchStatus, BatchStatus[]> AllowedTransitions = new()
    {
        [BatchStatus.InProduction] = [BatchStatus.AwaitingInspection],
        [BatchStatus.AwaitingInspection] = [BatchStatus.UnderInspection, BatchStatus.InProduction],
        [BatchStatus.UnderInspection] = [BatchStatus.Accepted, BatchStatus.ReworkRequired, BatchStatus.Rejected],
        [BatchStatus.ReworkRequired] = [BatchStatus.AwaitingInspection, BatchStatus.Rejected],
        [BatchStatus.Accepted] = [BatchStatus.Closed],
        [BatchStatus.Rejected] = [BatchStatus.Closed],
        [BatchStatus.Closed] = []
    };

    public bool CanTransitionTo(BatchStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Statuses reachable from <paramref name="from"/>. Lets a read model show the
    /// available moves without duplicating the transition table outside the domain.
    /// </summary>
    public static IReadOnlyList<BatchStatus> GetAllowedTransitions(BatchStatus from) =>
        AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : [];

    public void TransitionTo(BatchStatus target)
    {
        if (Status == target)
        {
            return;
        }

        if (!CanTransitionTo(target))
        {
            throw new DomainException(
                $"Недопустимый переход статуса партии {BatchNumber}: {Status} → {target}.");
        }

        Status = target;
    }

    /// <summary>
    /// Applies the verdict of a completed inspection to the batch.
    /// </summary>
    public void ApplyInspectionOutcome(InspectionOutcome outcome)
    {
        var target = outcome switch
        {
            InspectionOutcome.Conforming => BatchStatus.Accepted,
            InspectionOutcome.ReworkRequired => BatchStatus.ReworkRequired,
            InspectionOutcome.Defective => BatchStatus.Rejected,
            _ => throw new DomainException($"Неизвестный результат проверки: {outcome}.")
        };

        TransitionTo(target);
    }

    /// <summary>
    /// A closed batch is an archived record: no new inspections may be filed against it.
    /// </summary>
    public void EnsureCanBeInspected()
    {
        if (Status is BatchStatus.Closed)
        {
            throw new DomainException($"Партия {BatchNumber} закрыта, проверка невозможна.");
        }
    }
}
