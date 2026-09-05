namespace TrainingManagement.Api.Common.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = "ok";

    public T? Data { get; init; }

    public IReadOnlyCollection<ApiError>? Errors { get; init; }

    public string TraceId { get; init; } = string.Empty;

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
