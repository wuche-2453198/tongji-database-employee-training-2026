namespace TrainingManagement.Api.Entities;

public class CertificateCandidate
{
    public int RegId { get; set; }

    public int EmpId { get; set; }

    public string? EmployeeName { get; set; }

    public string? DepartmentName { get; set; }

    public int CourseId { get; set; }

    public string? CourseName { get; set; }

    public decimal? ActualHours { get; set; }
}
