using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Common.Exceptions;

public class BusinessException : Exception
{
    public int StatusCode { get; }

    public IReadOnlyCollection<ApiError> Errors { get; }

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
