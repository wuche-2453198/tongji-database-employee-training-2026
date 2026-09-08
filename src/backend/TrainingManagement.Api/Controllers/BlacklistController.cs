using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

// 黑名单管理接口
[Route("api/blacklists")]
[Authorize]
public sealed class BlacklistController : ApiControllerBase
{
    private readonly IBlacklistService _service;

    public BlacklistController(IBlacklistService service)
    {
        _service = service;
    }

    // 分页查询黑名单列表
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<BlacklistResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<BlacklistResponse>>>> GetList(
        [FromQuery] BlacklistQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(query, cancellationToken);
        return OkResponse(result);
    }

    // 根据ID获取黑名单记录详情
    [HttpGet("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BlacklistResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BlacklistResponse>>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }
        return OkResponse(result);
    }

    // 新增黑名单记录
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BlacklistResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BlacklistResponse>>> Create(
        [FromBody] CreateBlacklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return OkResponse(result);
    }

    // 修改黑名单记录
    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<BlacklistResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<BlacklistResponse>>> Update(
        long id,
        [FromBody] UpdateBlacklistRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return OkResponse(result);
    }

    // 删除黑名单记录
    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return OkResponse(result);
    }
}