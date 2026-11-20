namespace QualityControl.Domain.Common;

/// <summary>
/// Base type for every persisted entity in the domain.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
