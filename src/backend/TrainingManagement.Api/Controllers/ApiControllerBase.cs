using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T? data, string message = "ok")
    {
        return Ok(ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier, message));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(
        string? actionName,
        object? routeValues,
        T? data,
        string message = "created")
    {
        return CreatedAtAction(
            actionName,
            routeValues,
            ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier, message));
    }
}
