namespace TrainingManagement.Api.Entities;
public class TrainingCertificate
{
    public int CertId { get; set; }
    public int EmpId { get; set; }
    public int CourseId { get; set; }
    public string CertCode { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string Notified { get; set; } = "N";
    public DateTime? NotifiedAt { get; set; }
    public int IssuedByEmpId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CourseName { get; set; }
    public string? EmployeeName { get; set; }
}
