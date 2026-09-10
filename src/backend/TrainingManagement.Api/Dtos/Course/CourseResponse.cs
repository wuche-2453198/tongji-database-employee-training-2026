namespace TrainingManagement.Api.Dtos.Course;

public sealed class CourseResponse
{
    public long CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string CourseType { get; set; } = string.Empty;

    public decimal DurationHours { get; set; }

    public long? TrainerId { get; set; }

    public string? TrainerName { get; set; }

    public int MaxStudents { get; set; }

    public int? RegisteredCount { get; set; }

    public int? RemainingSeats { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public string? Location { get; set; }

    public string CourseStatus { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }

    public long? DeptId { get; set; }

    public string? DeptName { get; set; }

    public string? PreTestUrl { get; set; }

    public string? PostTestUrl { get; set; }

    public string? MaterialUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}