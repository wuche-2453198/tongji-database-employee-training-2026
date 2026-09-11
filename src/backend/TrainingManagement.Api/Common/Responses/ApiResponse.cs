namespace TrainingManagement.Api.Common.Responses;

/// <summary>
/// 所有接口共用的响应外壳，统一承载成功标记、数据、错误和追踪编号。
/// Controller、模型校验、JWT 事件和异常中间件都使用该结构，前端只需维护一套解析逻辑。
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = "ok";

    public T? Data { get; init; }

    public IReadOnlyCollection<ApiError>? Errors { get; init; }

    public string TraceId { get; init; } = string.Empty;

    /// <summary>创建成功响应对象，保留业务数据、提示信息和追踪编号。</summary>
    public static ApiResponse<T> Ok(T? data, string traceId, string message = "ok")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId
        };
    }

    /// <summary>创建失败响应对象，未提供字段错误时使用空列表。</summary>
    public static ApiResponse<T> Fail(
        string message,
        string traceId,
        IReadOnlyCollection<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? Array.Empty<ApiError>(),
            TraceId = traceId
        };
    }
}
