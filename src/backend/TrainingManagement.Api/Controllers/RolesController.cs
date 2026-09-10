using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Roles;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/roles")]
public sealed class RolesController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>返回角色及权限列表；控制器级策略限制为管理员访问。</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<RoleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleResponse>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllAsync(cancellationToken);
        return OkResponse(roles);
    }
}
