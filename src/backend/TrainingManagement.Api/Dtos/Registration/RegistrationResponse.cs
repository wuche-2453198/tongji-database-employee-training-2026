using TrainingManagement.Api.Dtos.Attendance;
namespace TrainingManagement.Api.Dtos.Registration;

public sealed class RegistrationResponse
{
    public long RegId { get; set; }

    public long RequestId { get; set; }

    public long EmpId { get; set; }

    public string EmpName { get; set; } = string.Empty;

    public long DeptId { get; set; }

    public string DeptName { get; set; } = string.Empty;

    public long CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string CourseType { get; set; } = string.Empty;

    public decimal DurationHours { get; set; }

    public string? TrainerName { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public string? Location { get; set; }

    public string CourseStatus { get; set; } = string.Empty;

    public int MaxStudents { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public decimal? ActualHours { get; set; }

    public DateTime? CanceledAt { get; set; }

    public string? CancelReason { get; set; }

    public AttendanceResponse? Attendance { get; set; }

    public RegistrationActions Actions { get; set; } = new();
}
