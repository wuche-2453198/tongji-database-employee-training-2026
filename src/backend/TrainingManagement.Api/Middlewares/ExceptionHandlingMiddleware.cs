using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;

namespace TrainingManagement.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException exception)
        {
            _logger.LogWarning(
                exception,
                "Business exception. TraceId: {TraceId}, Path: {Path}",
                context.TraceIdentifier,
                context.Request.Path);

            await context.WriteErrorResponseAsync(
                exception.StatusCode,
                exception.Message,
                exception.Errors);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
                context.TraceIdentifier,
                context.Request.Path);

            await context.WriteErrorResponseAsync(
                StatusCodes.Status500InternalServerError,
                "Internal server error.");
        }
    }
}
