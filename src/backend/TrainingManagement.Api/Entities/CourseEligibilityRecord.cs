namespace TrainingManagement.Api.Entities;

public sealed class CourseEligibilityRecord
{
    public long CourseId { get; init; }

    public string CourseStatus { get; init; } = string.Empty;

    public DateTime? StartAt { get; init; }

    public DateTime? EndAt { get; init; }

    public decimal DurationHours { get; init; }

    public int MaxStudents { get; init; }
}
