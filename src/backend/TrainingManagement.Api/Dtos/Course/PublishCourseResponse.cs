namespace TrainingManagement.Api.Dtos.Course;

public sealed class PublishCourseResponse
{
    public bool Published { get; set; } = true;

    public long CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public string CourseStatus { get; set; } = string.Empty;

    public int MaxStudents { get; set; }

    public int RegisteredCount { get; set; }

    public int RemainingSeats { get; set; }

    public DateTime? PublishTime { get; set; }
}
