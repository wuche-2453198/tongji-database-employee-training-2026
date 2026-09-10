namespace TrainingManagement.Api.Dtos.Certificates;

public class CertificateQueryDto
{
    public string? EmployeeName { get; set; }

    public string? CourseName { get; set; }

    public DateTime? StartDateFrom { get; set; }

    public DateTime? StartDateTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
