namespace TrainingManagement.Api.Common.Exceptions;

public sealed class NotFoundApiException : BusinessException
{
    public NotFoundApiException(string message = "Resource not found.")
        : base(message, StatusCodes.Status404NotFound)
    {
    }
}
