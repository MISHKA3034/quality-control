namespace QualityControl.Application.Common.Exceptions;

/// <summary>
/// Input failed validation. Carries per-field messages so the API can return an
/// RFC 7807 ValidationProblemDetails. Surfaces as HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException()
        : base("Обнаружены ошибки валидации.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
