namespace TrainingManagement.Api.Dtos.TrainingRequest;

public class TrainingRequestQueryDto
{
    public string? Status { get; set; }

    public int? EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public int? CourseId { get; set; }

    public string? CourseName { get; set; }

    public string? DepartmentName { get; set; }

    public DateTime? StartDateFrom { get; set; }

    public DateTime? StartDateTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
