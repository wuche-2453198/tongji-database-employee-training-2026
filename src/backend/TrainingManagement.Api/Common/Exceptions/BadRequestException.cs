using Microsoft.AspNetCore.Http;

namespace TrainingManagement.Api.Common.Exceptions;

public class BadRequestException : BusinessException
{
    public BadRequestException(string message) : base(message, StatusCodes.Status400BadRequest)
    {
    }
}
