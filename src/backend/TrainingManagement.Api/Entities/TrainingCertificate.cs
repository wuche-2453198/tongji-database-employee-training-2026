namespace TrainingManagement.Api.Entities;
public class TrainingCertificate
{
    private static readonly TimeSpan ExpiringSoonWindow = TimeSpan.FromDays(30);

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

    // 有效状态：EXPIRE_DATE 为空表示长期有效；否则按到期日判定。
    public string Status
    {
        get
        {
            if (!ExpireDate.HasValue)
            {
                return "VALID";
            }

            var today = DateTime.Today;
            var expire = ExpireDate.Value.Date;
            if (expire < today)
            {
                return "EXPIRED";
            }

            return expire <= today.Add(ExpiringSoonWindow) ? "EXPIRING" : "VALID";
        }
    }
}
