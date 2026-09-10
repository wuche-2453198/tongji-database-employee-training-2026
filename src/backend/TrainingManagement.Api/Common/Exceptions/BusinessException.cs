using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Common.Exceptions;

public class BusinessException : Exception
{
    public int StatusCode { get; }

    public IReadOnlyCollection<ApiError> Errors { get; }

    /// <summary>携带业务错误消息、HTTP 状态码和字段错误，交由异常中间件统一处理。</summary>
    public BusinessException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        IReadOnlyCollection<ApiError>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? Array.Empty<ApiError>();
    }
}
