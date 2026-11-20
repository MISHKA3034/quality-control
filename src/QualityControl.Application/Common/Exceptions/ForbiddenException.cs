namespace QualityControl.Application.Common.Exceptions;

/// <summary>
/// The caller is authenticated but not allowed to perform this action.
/// Used for rules that role attributes cannot express — for example, an inspector
/// editing an act signed by a different inspector. Surfaces as HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
