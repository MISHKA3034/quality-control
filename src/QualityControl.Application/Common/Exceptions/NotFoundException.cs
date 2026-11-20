namespace QualityControl.Application.Common.Exceptions;

/// <summary>
/// The requested entity does not exist (or is soft-deleted). Surfaces as HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} с идентификатором «{key}» не найден.")
    {
    }
}
