namespace QualityControl.Domain.Exceptions;

/// <summary>
/// Raised when an operation would violate a business rule owned by the domain
/// (an illegal status transition, inspecting a closed batch, and so on).
/// Surfaces to the client as HTTP 409 Conflict.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
