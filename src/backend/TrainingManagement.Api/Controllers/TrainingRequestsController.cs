using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[ApiController]
[Route("api/training-requests")]
public sealed class TrainingRequestsController : ApiControllerBase
{
    private readonly ITrainingRequestService _service;

    public TrainingRequestsController(ITrainingRequestService service)
    {
        _service = service;
    }

    /// <summary>员工提交培训申请。</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrainingRequestResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> Create(
        [FromBody] CreateTrainingRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var created = await _service.SubmitRequestAsync(actor.EmployeeId, dto, cancellationToken);

        return CreatedResponse(
            nameof(GetById),
            new { id = created.Id },
            created,
            "申请提交成功");
    }

    /// <summary>员工查看本人的申请(分页)。</summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TrainingRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<TrainingRequestResponseDto>>>> GetMy(
        [FromQuery] TrainingRequestQueryDto query, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var page = await _service.GetMyRequestsAsync(actor.EmployeeId, query, cancellationToken);

        return OkResponse(page);
    }

    /// <summary>主管/HR/管理员查询申请(主管仅本部门,HR/管理员跨部门)。</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ManagerHrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TrainingRequestResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<TrainingRequestResponseDto>>>> GetAll(
        [FromQuery] TrainingRequestQueryDto query, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var page = await _service.GetAllRequestsAsync(actor, query, cancellationToken);

        return OkResponse(page);
    }

    /// <summary>查看申请详情(员工本人/本部门主管/HR/管理员)。</summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrainingRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> GetById(
        int id, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var request = await _service.GetRequestByIdAsync(actor, id, cancellationToken);

        return OkResponse(request);
    }

    /// <summary>主管审批通过(管理员可代办)。</summary>
    [HttpPatch("{id:int}/dept-approve")]
    [Authorize(Roles = RoleCodes.DepartmentManager + "," + RoleCodes.Admin)]
    [ProducesResponseType(typeof(ApiResponse<TrainingRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> DeptApprove(
        int id, [FromBody] ApproveRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var request = await _service.DeptApproveAsync(actor, id, dto.Comment, cancellationToken);

        return OkResponse(request, "审批通过");
    }

    /// <summary>主管驳回(管理员可代办)。</summary>
    [HttpPatch("{id:int}/dept-reject")]
    [Authorize(Roles = RoleCodes.DepartmentManager + "," + RoleCodes.Admin)]
    [ProducesResponseType(typeof(ApiResponse<TrainingRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> DeptReject(
        int id, [FromBody] ApproveRequestDto dto, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var request = await _service.DeptRejectAsync(actor, id, dto.Comment, cancellationToken);

        return OkResponse(request, "已驳回");
    }

    /// <summary>HR 备案,记录备案意见。</summary>
    [HttpPatch("{id:int}/hr-file")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<TrainingRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TrainingRequestResponseDto>>> HrFile(
        int id, [FromBody] HrFileRequestDto? dto, CancellationToken cancellationToken)
    {
        var actor = GetActor();
        var request = await _service.HrFileAsync(actor, id, dto?.Comment, cancellationToken);

        return OkResponse(request, "备案完成");
    }

    private ActorContext GetActor()
    {
        var employeeIdClaim = User.FindFirstValue(TrainingClaimTypes.EmployeeId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(employeeIdClaim, out var employeeId))
        {
            throw new UnauthorizedApiException("无法获取当前用户身份。");
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

        return new ActorContext(employeeId, roles);
    }
}
