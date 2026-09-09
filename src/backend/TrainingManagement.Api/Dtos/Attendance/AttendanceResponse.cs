namespace TrainingManagement.Api.Dtos.Attendance;

public sealed class AttendanceResponse
{
    public long AttendId { get; set; }

    public long RegId { get; set; }

    public string SigninType { get; set; } = string.Empty;

    public DateTime SignedInAt { get; set; }

    public int LatenessMinutes { get; set; }

    public decimal DeductHours { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }
}
