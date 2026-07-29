namespace TrainingManagement.Api.Common.Exceptions;

public sealed class ConflictApiException : BusinessException
{
    public ConflictApiException(string message = "Data conflict.")
        : base(message, StatusCodes.Status409Conflict)
    {
    }
}
