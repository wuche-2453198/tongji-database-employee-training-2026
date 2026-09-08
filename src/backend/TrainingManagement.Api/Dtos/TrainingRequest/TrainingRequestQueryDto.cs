namespace TrainingManagement.Api.Dtos.TrainingRequest;

public class TrainingRequestQueryDto
{
    public string? Status { get; set; }

    public int? EmployeeId { get; set; }

    public int? CourseId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
