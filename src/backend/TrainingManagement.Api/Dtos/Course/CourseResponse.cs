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

    /// <summary>有效报名数（REGISTERED/SIGNED_IN/COMPLETED/ABSENT），供前端展示剩余名额。</summary>
    public int? RegisteredCount { get; set; }

    /// <summary>剩余名额，由最大人数减去有效报名数得到。</summary>
    public int? RemainingSeats { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}