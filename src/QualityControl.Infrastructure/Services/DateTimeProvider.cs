using QualityControl.Application.Common.Interfaces;

namespace QualityControl.Infrastructure.Services;

/// <inheritdoc cref="IDateTimeProvider"/>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
