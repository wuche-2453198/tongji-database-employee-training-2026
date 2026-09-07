using Microsoft.AspNetCore.Http;

namespace TrainingManagement.Api.Common.Exceptions;

public class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message, StatusCodes.Status409Conflict)
    {
    }
}
