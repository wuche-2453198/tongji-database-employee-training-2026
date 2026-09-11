using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Health;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ApiControllerBase
{
    private readonly IHealthService _healthService;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>提供应用存活信息，不执行数据库查询。</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SystemHealthResponse>> Get()
    {
        return OkResponse(_healthService.GetSystemHealth());
    }

    /// <summary>探测 Oracle 数据库，成功返回 200，未配置或连接失败返回 503。</summary>
    [HttpGet("db")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseHealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<DatabaseHealthResponse>>> Database(
        CancellationToken cancellationToken)
    {
        var result = await _healthService.GetDatabaseHealthAsync(cancellationToken);

        if (result.Connected)
        {
            return OkResponse(result);
        }

        var response = ApiResponse<object>.Fail(
            result.Message ?? "Database health check failed.",
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
