using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>将业务数据包装为带请求追踪编号的统一 HTTP 200 响应。</summary>
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T? data, string message = "ok")
    {
        return Ok(ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier, message));
    }

    /// <summary>返回统一 HTTP 201 响应，并根据目标动作和路由参数生成资源地址。</summary>
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
