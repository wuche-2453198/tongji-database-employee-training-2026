namespace TrainingManagement.Api.Dtos.Auth;

/// <summary>对外返回的用户资料及角色权限，不包含密码哈希。</summary>
public sealed class AuthUserResponse
{
    public long EmpId { get; init; }

    public string EmpName { get; init; } = string.Empty;

    public string? DeptName { get; init; }

    public string? Position { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyCollection<AuthRoleResponse> Roles { get; init; } =
        Array.Empty<AuthRoleResponse>();

    public IReadOnlyCollection<string> Permissions { get; init; } =
        Array.Empty<string>();
}
