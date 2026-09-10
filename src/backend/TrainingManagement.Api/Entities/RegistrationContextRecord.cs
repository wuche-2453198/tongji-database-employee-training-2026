namespace TrainingManagement.Api.Entities;

/// <summary>
/// 报名记录与课程关键字段的只读投影，供 Service 做状态流转和资格校验。
/// </summary>
public sealed class RegistrationContextRecord
{
    public long RegId { get; init; }

    public long RequestId { get; init; }

    public long EmpId { get; init; }

    public long CourseId { get; init; }

    public string Status { get; init; } = string.Empty;

    public decimal? ActualHours { get; init; }

    public string CourseStatus { get; init; } = string.Empty;

    public DateTime? StartAt { get; init; }

    public DateTime? EndAt { get; init; }

    public decimal DurationHours { get; init; }

    public int MaxStudents { get; init; }
}
