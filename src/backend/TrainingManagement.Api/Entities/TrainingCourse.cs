namespace TrainingManagement.Api.Entities;

public sealed class TrainingCourse
{
    public long CourseId { get; init; }

    public string CourseName { get; init; } = string.Empty;

    public string CourseType { get; init; } = string.Empty;

    public decimal DurationHours { get; init; }

    public long? TrainerId { get; init; }

    public string? TrainerName { get; init; }

    public int MaxStudents { get; init; }

    public DateTime? StartAt { get; init; }

    public DateTime? EndAt { get; init; }

    public string? Location { get; init; }

    public string CourseStatus { get; init; } = string.Empty;

    public decimal BudgetAmount { get; init; }

    public long? DeptId { get; init; }

    public string? DeptName { get; init; }

    public string? PreTestUrl { get; init; }

    public string? PostTestUrl { get; init; }

    public string? MaterialUrl { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}