using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;

namespace TrainingManagement.Api.Middlewares;

/// <summary>
/// 全局异常处理中间件：位于控制器外层，将业务异常和未知异常转换为统一 JSON 响应。
/// 每条错误响应都沿用当前请求的 TraceId，便于前端反馈与后端日志相互定位。
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>保存后续中间件委托和日志组件，形成请求管道中的异常处理节点。</summary>
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
            // 可预期的业务错误保留异常中指定的 4xx 状态码和字段错误。
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
            // 未分类异常不向客户端暴露内部细节，完整异常只写入服务端日志。
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
