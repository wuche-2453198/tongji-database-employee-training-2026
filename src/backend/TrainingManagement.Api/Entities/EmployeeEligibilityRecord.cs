namespace TrainingManagement.Api.Entities;

public sealed class EmployeeEligibilityRecord
{
    public long EmpId { get; init; }

    public string Status { get; init; } = string.Empty;

    public int ActiveBlacklistCount { get; init; }
}
