using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Security role. The rows are fixed reference data seeded by migration; the
/// well-known names live in <see cref="RoleNames"/>.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>
/// The role names used in <c>[Authorize(Roles = ...)]</c> and in JWT role claims.
/// </summary>
public static class RoleNames
{
    public const string Administrator = nameof(Administrator);
    public const string QualityInspector = nameof(QualityInspector);
    public const string WorkshopManager = nameof(WorkshopManager);
    public const string ProductionManager = nameof(ProductionManager);

    public static readonly IReadOnlyList<string> All =
    [
        Administrator,
        QualityInspector,
        WorkshopManager,
        ProductionManager
    ];
}
