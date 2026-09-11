using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Dashboard;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats(CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var result = await _service.GetStatsAsync(actor, cancellationToken);
        return OkResponse(result, "查询成功");
    }

    private ActorContext GetActor()
    {
        var employeeId = User.GetEmployeeId()
            ?? throw new UnauthorizedApiException("无法获取当前用户身份。");
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return new ActorContext((int)employeeId, roles);
    }
}
