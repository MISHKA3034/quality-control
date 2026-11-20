using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Domain.Exceptions;
using ValidationException = QualityControl.Application.Common.Exceptions.ValidationException;

namespace QualityControl.API.Middleware;

/// <summary>
/// Turns exceptions into RFC 7807 problem responses.
/// </summary>
/// <remarks>
/// Centralising this is what lets the services throw meaningful exceptions instead
/// of every controller repeating try/catch and inventing its own status codes.
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        // The client has gone; there is nobody to answer and nothing went wrong.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Запрос {Path} отменён клиентом.", context.Request.Path);
            return;
        }

        // Once the response has started, the status line is already on the wire and
        // rewriting it would corrupt the response.
        if (context.Response.HasStarted)
        {
            _logger.LogError(
                exception,
                "Исключение после начала ответа на {Path}. Ответ невозможно изменить.",
                context.Request.Path);

            return;
        }

        var problem = BuildProblem(context, exception);

        context.Response.Clear();
        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        // Serialised against the runtime type on purpose. System.Text.Json uses the
        // declared type, so passing this as ProblemDetails would silently drop
        // ValidationProblemDetails.Errors — the per-field messages the client needs.
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, problem.GetType(), JsonOptions),
            context.RequestAborted);
    }

    private ProblemDetails BuildProblem(HttpContext context, Exception exception)
    {
        // Lets a user quoting an error be tied to the exact request in the logs.
        var traceId = context.TraceIdentifier;

        switch (exception)
        {
            case ValidationException validation:
                _logger.LogInformation(
                    "Ошибка валидации на {Path}: {Errors}",
                    context.Request.Path,
                    string.Join("; ", validation.Errors.SelectMany(e => e.Value)));

                return new ValidationProblemDetails(validation.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ошибка валидации",
                    Detail = "Проверьте правильность заполнения полей.",
                    Instance = context.Request.Path,
                    Extensions = { ["traceId"] = traceId }
                };

            case NotFoundException notFound:
                _logger.LogInformation("Не найдено на {Path}: {Message}", context.Request.Path, notFound.Message);

                return Problem(
                    StatusCodes.Status404NotFound,
                    "Ресурс не найден",
                    notFound.Message,
                    context,
                    traceId);

            case ForbiddenException forbidden:
                _logger.LogWarning(
                    "Доступ запрещён на {Path}: {Message}",
                    context.Request.Path,
                    forbidden.Message);

                return Problem(
                    StatusCodes.Status403Forbidden,
                    "Доступ запрещён",
                    forbidden.Message,
                    context,
                    traceId);

            case DomainException domain:
                // A broken business rule is the caller asking for something the
                // domain forbids — a conflict, not a server fault.
                _logger.LogInformation(
                    "Нарушение бизнес-правила на {Path}: {Message}",
                    context.Request.Path,
                    domain.Message);

                return Problem(
                    StatusCodes.Status409Conflict,
                    "Операция недопустима",
                    domain.Message,
                    context,
                    traceId);

            case UnauthorizedAccessException:
                return Problem(
                    StatusCodes.Status403Forbidden,
                    "Доступ запрещён",
                    "Недостаточно прав для выполнения операции.",
                    context,
                    traceId);

            default:
                _logger.LogError(
                    exception,
                    "Необработанное исключение на {Path}. TraceId: {TraceId}",
                    context.Request.Path,
                    traceId);

                return Problem(
                    StatusCodes.Status500InternalServerError,
                    "Внутренняя ошибка сервера",

                    // Real exception text only outside production: messages routinely
                    // carry table names, paths and connection details.
                    _environment.IsProduction()
                        ? $"Произошла непредвиденная ошибка. Сообщите администратору код: {traceId}"
                        : exception.ToString(),
                    context,
                    traceId);
        }
    }

    private static ProblemDetails Problem(
        int status,
        string title,
        string detail,
        HttpContext context,
        string traceId) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = traceId }
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
