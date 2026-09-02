namespace TrainingManagement.Api.Entities;
public class TrainingCertificate
{
    public int CertificateId { get; set; }
    public string CertificateNo { get; set; }  // CERT-{yyyyMMdd}-{courseId}-{empId}
    public int EmployeeId { get; set; }
    public int CourseId { get; set; }
    public int RegistrationId { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string NotifyFlag { get; set; } = "N";
}
