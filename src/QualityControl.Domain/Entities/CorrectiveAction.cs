using QualityControl.Domain.Common;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Корректирующее действие — the work item that tracks a defect through to closure.
/// Owns its own status machine: callers ask for a transition and the entity decides
/// whether it is legal, stamps the timestamps and appends a history record.
/// </summary>
public class CorrectiveAction : AuditableEntity, ISoftDeletable
{
    public int DefectId { get; set; }

    public Defect Defect { get; set; } = null!;

    public string Description { get; set; } = null!;

    public CorrectiveActionStatus Status { get; private set; } = CorrectiveActionStatus.New;

    /// <summary>Ответственный за устранение.</summary>
    public int? AssignedToId { get; private set; }

    public Employee? AssignedTo { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? AssignedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? FixedAt { get; private set; }

    public DateTime? VerifiedAt { get; private set; }

    public int? VerifiedByEmployeeId { get; private set; }

    public Employee? VerifiedByEmployee { get; set; }

    public DateTime? ClosedAt { get; private set; }

    /// <summary>Описание фактически выполненных работ.</summary>
    public string? ResolutionNotes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedByUserId { get; set; }

    public ICollection<CorrectiveActionHistory> History { get; set; } = new List<CorrectiveActionHistory>();

    /// <summary>Просрочено — открыто и срок исполнения прошёл.</summary>
    public bool IsOverdue(DateTime utcNow) =>
        DueDate is not null
        && Status != CorrectiveActionStatus.Closed
        && utcNow.Date > DueDate.Value.Date;

    private static readonly Dictionary<CorrectiveActionStatus, CorrectiveActionStatus[]> AllowedTransitions = new()
    {
        [CorrectiveActionStatus.New] = [CorrectiveActionStatus.Assigned],
        [CorrectiveActionStatus.Assigned] = [CorrectiveActionStatus.InProgress, CorrectiveActionStatus.New],
        [CorrectiveActionStatus.InProgress] = [CorrectiveActionStatus.Fixed],

        // Проверка ОТК может вернуть работу исполнителю, если дефект устранён не полностью.
        [CorrectiveActionStatus.Fixed] = [CorrectiveActionStatus.Verified, CorrectiveActionStatus.InProgress],
        [CorrectiveActionStatus.Verified] = [CorrectiveActionStatus.Closed, CorrectiveActionStatus.InProgress],
        [CorrectiveActionStatus.Closed] = []
    };

    public bool CanTransitionTo(CorrectiveActionStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Statuses reachable from <paramref name="from"/>. Lets a read model show the
    /// available moves without duplicating the transition table outside the domain.
    /// </summary>
    public static IReadOnlyList<CorrectiveActionStatus> GetAllowedTransitions(CorrectiveActionStatus from) =>
        AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : [];

    /// <summary>
    /// Назначить ответственного. Moves a new action to <see cref="CorrectiveActionStatus.Assigned"/>;
    /// on an action already in flight it only changes the assignee.
    /// </summary>
    public void AssignTo(int employeeId, int changedByUserId, DateTime utcNow, string? comment = null)
    {
        if (Status == CorrectiveActionStatus.Closed)
        {
            throw new DomainException("Нельзя изменить ответственного у закрытого корректирующего действия.");
        }

        AssignedToId = employeeId;
        AssignedAt = utcNow;

        if (Status == CorrectiveActionStatus.New)
        {
            ChangeStatus(CorrectiveActionStatus.Assigned, changedByUserId, utcNow, comment);
        }
    }

    /// <summary>
    /// Moves the action to <paramref name="target"/>, recording the change in <see cref="History"/>.
    /// </summary>
    /// <param name="target">Requested new status.</param>
    /// <param name="changedByUserId">Account performing the transition.</param>
    /// <param name="utcNow">Current UTC time, supplied by the caller so the entity stays testable.</param>
    /// <param name="comment">Optional note stored on the history record.</param>
    /// <param name="verifiedByEmployeeId">
    /// Контролёр, принявший работу. Required when transitioning to
    /// <see cref="CorrectiveActionStatus.Verified"/>.
    /// </param>
    /// <exception cref="DomainException">The transition is not allowed from the current status.</exception>
    public void ChangeStatus(
        CorrectiveActionStatus target,
        int changedByUserId,
        DateTime utcNow,
        string? comment = null,
        int? verifiedByEmployeeId = null)
    {
        if (Status == target)
        {
            throw new DomainException($"Корректирующее действие уже имеет статус «{target}».");
        }

        if (!CanTransitionTo(target))
        {
            throw new DomainException($"Недопустимый переход статуса: {Status} → {target}.");
        }

        if (target == CorrectiveActionStatus.Assigned && AssignedToId is null)
        {
            throw new DomainException("Нельзя перевести действие в статус «Назначен» без ответственного.");
        }

        if (target == CorrectiveActionStatus.Verified)
        {
            if (verifiedByEmployeeId is null)
            {
                throw new DomainException("Для перевода в статус «Проверен» необходимо указать контролёра.");
            }

            VerifiedByEmployeeId = verifiedByEmployeeId;
            VerifiedAt = utcNow;
        }

        var previous = Status;
        Status = target;

        switch (target)
        {
            case CorrectiveActionStatus.New:
                AssignedToId = null;
                AssignedAt = null;
                break;
            case CorrectiveActionStatus.InProgress:
                StartedAt ??= utcNow;

                // Работа возвращена исполнителю — прежняя приёмка больше не действует.
                FixedAt = null;
                VerifiedAt = null;
                VerifiedByEmployeeId = null;
                break;
            case CorrectiveActionStatus.Fixed:
                FixedAt = utcNow;
                break;
            case CorrectiveActionStatus.Closed:
                ClosedAt = utcNow;
                break;
        }

        History.Add(new CorrectiveActionHistory
        {
            CorrectiveAction = this,
            FromStatus = previous,
            ToStatus = target,
            ChangedByUserId = changedByUserId,
            ChangedAt = utcNow,
            Comment = comment
        });
    }

    /// <summary>
    /// Records the action's creation in its own history, so the timeline starts at
    /// the beginning rather than at the first transition.
    /// </summary>
    public void RecordCreation(int changedByUserId, DateTime utcNow)
    {
        History.Add(new CorrectiveActionHistory
        {
            CorrectiveAction = this,
            FromStatus = null,
            ToStatus = CorrectiveActionStatus.New,
            ChangedByUserId = changedByUserId,
            ChangedAt = utcNow,
            Comment = "Корректирующее действие создано."
        });
    }
}
