namespace TrainingManagement.Api.Common.Exceptions;

public sealed class UnauthorizedApiException : BusinessException
{
    public UnauthorizedApiException(string message = "Invalid credentials.")
        : base(message, StatusCodes.Status401Unauthorized)
    {
    }
}
