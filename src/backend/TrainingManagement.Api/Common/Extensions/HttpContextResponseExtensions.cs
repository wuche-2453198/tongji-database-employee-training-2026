using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Common.Extensions;

public static class HttpContextResponseExtensions
{
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
