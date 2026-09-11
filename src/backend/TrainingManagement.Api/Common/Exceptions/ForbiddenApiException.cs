namespace TrainingManagement.Api.Common.Exceptions;

public sealed class ForbiddenApiException : BusinessException
{
    /// <summary>表示已识别身份但不允许执行操作，对应 HTTP 403。</summary>
    public ForbiddenApiException(string message = "Permission denied.")
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}
