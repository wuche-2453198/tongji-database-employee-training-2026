using Microsoft.Extensions.Options;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Roles;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly AuthOptions _authOptions;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public RoleService(
        IRoleRepository roleRepository,
        IOptions<AuthOptions> authOptions)
    {
        _roleRepository = roleRepository;
        _authOptions = authOptions.Value;
    }

    /// <summary>优先读取数据库角色；列表为空时使用配置推导的默认角色。</summary>
    public async Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var records = await _roleRepository.GetAllAsync(cancellationToken);

        if (records.Count > 0)
        {
            return records.Select(ToRoleResponse).ToArray();
        }

        return BuildDefaultRoleResponses();
    }

    /// <summary>将数据库角色转换为接口响应，统一角色代码并补充默认权限。</summary>
    private static RoleResponse ToRoleResponse(RoleRecord role)
    {
        var roleCode = RoleCodes.Normalize(string.IsNullOrWhiteSpace(role.RoleCode)
            ? role.RoleName
            : role.RoleCode);

        return new RoleResponse
        {
            RoleId = role.RoleId,
            RoleCode = roleCode,
            RoleName = role.RoleName,
            Permissions = RolePermissionParser.Parse(role.Permissions, roleCode)
        };
    }

    /// <summary>数据库角色列表为空时，从演示配置推导角色；无配置时至少返回员工角色。</summary>
    private IReadOnlyCollection<RoleResponse> BuildDefaultRoleResponses()
    {
        var configuredRoles = _authOptions.LocalDemoUsers
            .SelectMany(user => user.Roles)
            .Select(RoleCodes.Normalize)
            .DefaultIfEmpty(RoleCodes.Employee)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return configuredRoles.Select((roleCode, index) => new RoleResponse
        {
            RoleId = index + 1,
            RoleCode = roleCode,
            RoleName = roleCode,
            Permissions = PermissionCodes.GetDefaultPermissions(roleCode)
        }).ToArray();
    }
}
