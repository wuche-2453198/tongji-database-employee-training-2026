namespace TrainingManagement.Api.Common.Exceptions;

public sealed class NotFoundApiException : BusinessException
{
    /// <summary>表示目标资源不存在，对应 HTTP 404。</summary>
    public NotFoundApiException(string message = "Resource not found.")
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}
