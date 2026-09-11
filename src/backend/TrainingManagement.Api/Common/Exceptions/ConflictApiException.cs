namespace TrainingManagement.Api.Common.Exceptions;

public sealed class ConflictApiException : BusinessException
{
    /// <summary>表示重复提交或状态冲突，对应 HTTP 409。</summary>
    public ConflictApiException(string message = "Data conflict.")
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}
