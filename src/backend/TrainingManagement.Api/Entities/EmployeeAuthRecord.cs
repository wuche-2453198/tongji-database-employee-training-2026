namespace TrainingManagement.Api.Entities;

public sealed class EmployeeAuthRecord
{
    public long EmpId { get; init; }

    public string LoginName { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string EmpName { get; init; } = string.Empty;

    public string? DeptName { get; init; }

    public string? Position { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? Status { get; init; }

    public DateTime? CreatedAt { get; init; }
}
