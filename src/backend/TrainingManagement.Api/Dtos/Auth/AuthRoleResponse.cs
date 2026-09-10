namespace TrainingManagement.Api.Dtos.Auth;

/// <summary>认证响应中的单个角色及其权限代码。</summary>
public sealed class AuthRoleResponse
{
    public long RoleId { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}
