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

    /// <summary>报名状态,取值与 TRAINING_REGISTRATIONS.STATUS 一致。</summary>
    public string Status { get; set; } = "UNKNOWN";

    /// <summary>是否满足发证条件(含训后测试门槛),Y/N。</summary>
    public string Qualified { get; set; } = "N";

    /// <summary>不满足发证条件时的原因。</summary>
    public string? QualificationReason { get; set; }
}
