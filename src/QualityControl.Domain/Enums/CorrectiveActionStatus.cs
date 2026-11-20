namespace QualityControl.Domain.Enums;

/// <summary>
/// Workflow state of a corrective action. Allowed transitions are enforced by
/// <see cref="Entities.CorrectiveAction"/> rather than by the caller.
/// </summary>
public enum CorrectiveActionStatus
{
    /// <summary>Новый.</summary>
    New = 1,

    /// <summary>Назначен ответственному.</summary>
    Assigned = 2,

    /// <summary>В работе.</summary>
    InProgress = 3,

    /// <summary>Исправлен, ожидает проверки ОТК.</summary>
    Fixed = 4,

    /// <summary>Проверен ОТК.</summary>
    Verified = 5,

    /// <summary>Закрыт.</summary>
    Closed = 6
}
