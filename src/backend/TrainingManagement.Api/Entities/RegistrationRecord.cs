namespace TrainingManagement.Api.Entities;

public sealed class RegistrationRecord
{
    public long RegId { get; init; }

    public long RequestId { get; init; }

    public long EmpId { get; init; }

    public string EmpName { get; init; } = string.Empty;

    public long DeptId { get; init; }

    public string DeptName { get; init; } = string.Empty;

    public long CourseId { get; init; }

    public string CourseName { get; init; } = string.Empty;

    public string CourseType { get; init; } = string.Empty;

    public decimal DurationHours { get; init; }

    public string? TrainerName { get; init; }

    public DateTime? StartAt { get; init; }

    public DateTime? EndAt { get; init; }

    public string? Location { get; init; }

    public string CourseStatus { get; init; } = string.Empty;

    public int MaxStudents { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime RegisteredAt { get; init; }

    public DateTime? CompletedAt { get; init; }

    public decimal? ActualHours { get; init; }

    public DateTime? CanceledAt { get; init; }

    public string? CancelReason { get; init; }

    public DateTime UpdatedAt { get; init; }

    public long? AttendId { get; init; }

    public string? SigninType { get; init; }

    public DateTime? SignedInAt { get; init; }

    public int? LatenessMinutes { get; init; }

    public decimal? DeductHours { get; init; }

    public string? AttendanceRemark { get; init; }
}
