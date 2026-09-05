namespace TrainingManagement.Api.Dtos.Auth;

public sealed class AuthRoleResponse
{
    public long RoleId { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Permissions { get; init; } = Array.Empty<string>();
}
