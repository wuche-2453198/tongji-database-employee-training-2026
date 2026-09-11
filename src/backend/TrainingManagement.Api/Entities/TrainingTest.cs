namespace TrainingManagement.Api.Entities;
public class TrainingTest
{
    public int TestId { get; set; }
    public int EmpId { get; set; }
    public int CourseId { get; set; }
    public string TestType { get; set; } = "";
    public decimal Score { get; set; }
    public int RecordedByEmpId { get; set; }
    public DateTime TestedAt { get; set; }
    public string? EmployeeName { get; set; }
    public string? CourseName { get; set; }
}
