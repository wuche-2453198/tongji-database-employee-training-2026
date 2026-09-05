namespace TrainingManagement.Api.Common.Exceptions;

public sealed class ForbiddenApiException : BusinessException
{
    public ForbiddenApiException(string message = "Permission denied.")
        : base(message, StatusCodes.Status403Forbidden)
    {
    }
}
