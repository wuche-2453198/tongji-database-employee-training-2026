using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

// 部门培训预算管理接口
[Route("api/department-trainings")]
[Authorize]
public sealed class DepartmentTrainingController : ApiControllerBase
{
    private readonly IDepartmentTrainingService _service;

    public DepartmentTrainingController(IDepartmentTrainingService service)
    {
        _service = service;
    }

    // 分页查询部门培训预算列表
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<DepartmentTrainingResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<DepartmentTrainingResponse>>>> GetList(
        [FromQuery] DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(query, cancellationToken);
        return OkResponse(result);
    }

    // 根据ID获取部门培训预算详情
    [HttpGet("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentTrainingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DepartmentTrainingResponse>>> GetById(
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

    // 新增部门培训预算
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentTrainingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DepartmentTrainingResponse>>> Create(
        [FromBody] CreateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return OkResponse(result);
    }

    // 修改部门培训预算
    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentTrainingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DepartmentTrainingResponse>>> Update(
        long id,
        [FromBody] UpdateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return OkResponse(result);
    }

    // 删除部门培训预算
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