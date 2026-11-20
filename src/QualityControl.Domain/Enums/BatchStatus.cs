namespace QualityControl.Domain.Enums;

/// <summary>
/// Lifecycle of a production batch as it moves through the shop floor and OTK.
/// </summary>
public enum BatchStatus
{
    /// <summary>Партия в производстве.</summary>
    InProduction = 1,

    /// <summary>Изготовлена, ожидает контроля ОТК.</summary>
    AwaitingInspection = 2,

    /// <summary>Проверка ОТК начата.</summary>
    UnderInspection = 3,

    /// <summary>Принята ОТК.</summary>
    Accepted = 4,

    /// <summary>Требуется доработка по результатам контроля.</summary>
    ReworkRequired = 5,

    /// <summary>Забракована.</summary>
    Rejected = 6,

    /// <summary>Закрыта, дальнейшие изменения запрещены.</summary>
    Closed = 7
}
