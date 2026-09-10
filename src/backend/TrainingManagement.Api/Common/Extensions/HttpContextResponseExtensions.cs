using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Common.Extensions;

public static class HttpContextResponseExtensions
{
    /// <summary>在控制器外写入统一成功 JSON，附带当前请求追踪编号。</summary>
    public static Task WriteSuccessResponseAsync<T>(
        this HttpContext httpContext,
        T? data,
        string message = "ok",
        int statusCode = StatusCodes.Status200OK)
    {
        httpContext.Response.StatusCode = statusCode;
        return httpContext.Response.WriteAsJsonAsync(
            ApiResponse<T>.Ok(data, httpContext.TraceIdentifier, message));
    }

    /// <summary>在中间件或认证事件中写入统一错误 JSON 和指定 HTTP 状态码。</summary>
    public static Task WriteErrorResponseAsync(
        this HttpContext httpContext,
        int statusCode,
        string message,
        IReadOnlyCollection<ApiError>? errors = null)
    {
        httpContext.Response.StatusCode = statusCode;
        return httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(message, httpContext.TraceIdentifier, errors));
    }
}
