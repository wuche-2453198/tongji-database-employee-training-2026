namespace TrainingManagement.Api.Dtos.Auth;

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
