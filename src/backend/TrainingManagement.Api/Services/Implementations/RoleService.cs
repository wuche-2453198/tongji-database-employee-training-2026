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

    public RoleService(
        IRoleRepository roleRepository,
        IOptions<AuthOptions> authOptions)
    {
        _roleRepository = roleRepository;
        _authOptions = authOptions.Value;
    }

    public async Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var records = await _roleRepository.GetAllAsync(cancellationToken);

        if (records.Count > 0)
        {
            return records.Select(ToRoleResponse).ToArray();
        }

        return BuildDefaultRoleResponses();
    }

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
