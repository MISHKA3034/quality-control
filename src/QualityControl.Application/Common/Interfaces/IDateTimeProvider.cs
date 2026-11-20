namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Abstracts the clock so time-dependent rules (token expiry, overdue actions)
/// are testable and do not reach for <see cref="DateTime.UtcNow"/> directly.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Current time in UTC. Everything is stored in UTC and converted at the edge.</summary>
    DateTime UtcNow { get; }

    DateOnly Today { get; }
}
