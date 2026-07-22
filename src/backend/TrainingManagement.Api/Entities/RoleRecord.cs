namespace TrainingManagement.Api.Entities;

public sealed class RoleRecord
{
    public long RoleId { get; init; }

    public string RoleName { get; init; } = string.Empty;

    public string? Permissions { get; init; }
}
