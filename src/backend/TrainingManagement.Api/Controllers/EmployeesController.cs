using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

// 员工管理接口
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // 分页查询员工列表
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeResponse>>>> GetEmployees(
        [FromQuery] EmployeeQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetPagedAsync(query, cancellationToken);
        return OkResponse(result);
    }

    // 根据ID获取员工详情
    [HttpGet("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> GetEmployeeById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }
        return OkResponse(result);
    }

    // 新增员工
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.CreateAsync(request, cancellationToken);
        return OkResponse(result);
    }

    // 修改员工信息
    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EmployeeResponse>>> UpdateEmployee(
        long id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.UpdateAsync(id, request, cancellationToken);
        return OkResponse(result);
    }

    // 删除员工（软删除，状态改为离职）
    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEmployee(
        long id,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.DeleteAsync(id, cancellationToken);
        return OkResponse(result);
    }
}