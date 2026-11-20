using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = QualityControl.Application.Common.Exceptions.ValidationException;

namespace QualityControl.API.Filters;

/// <summary>
/// Validates action arguments against their registered FluentValidation validators
/// before the action runs.
/// </summary>
/// <remarks>
/// Applied globally, so a controller cannot forget to validate: adding a validator
/// for a request type is enough to have it enforced. Failures are thrown as the
/// Application layer's <see cref="ValidationException"/> and rendered by
/// <see cref="Middleware.ExceptionHandlingMiddleware"/>, so validation errors look
/// the same whether they came from here or from deeper in a service.
/// </remarks>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            // Resolved by runtime type: the validator is registered against the
            // concrete request type, which the parameter's static type may not be.
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        await next();
    }
}
