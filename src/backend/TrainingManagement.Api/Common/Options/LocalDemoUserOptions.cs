namespace TrainingManagement.Api.Common.Options;

/// <summary>本地演示用户资料，仅用于显式开启演示认证的场景。</summary>
public sealed class LocalDemoUserOptions
{
    public long EmpId { get; init; }

    public string EmpName { get; init; } = string.Empty;

    public string? DeptName { get; init; }

    public string? Position { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string Status { get; init; } = "ACTIVE";

    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
