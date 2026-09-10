namespace TrainingManagement.Api.Entities;

public sealed class AttendanceRecord
{
    public long AttendId { get; init; }

    public long RegId { get; init; }

    public string SigninType { get; init; } = string.Empty;

    public DateTime SignedInAt { get; init; }

    public int LatenessMinutes { get; init; }

    public decimal DeductHours { get; init; }

    public string? Remark { get; init; }

    public DateTime CreatedAt { get; init; }
}
