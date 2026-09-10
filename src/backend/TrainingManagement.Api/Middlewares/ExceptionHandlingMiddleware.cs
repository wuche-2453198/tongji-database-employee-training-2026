using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;

namespace TrainingManagement.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>执行后续请求管道；业务异常按指定状态返回，未知异常记录日志并返回统一 500。</summary>
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
