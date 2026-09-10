namespace TrainingManagement.Api.Dtos.Roles;

/// <summary>角色查询接口的返回结构。</summary>
public sealed class RoleResponse
{
    public long RoleId { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}
