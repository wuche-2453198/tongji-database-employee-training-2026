namespace TrainingManagement.Api.Common.Exceptions;

public sealed class UnauthorizedApiException : BusinessException
{
    /// <summary>表示凭据无效或缺少认证，对应 HTTP 401。</summary>
    public UnauthorizedApiException(string message = "Invalid credentials.")
        : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}
