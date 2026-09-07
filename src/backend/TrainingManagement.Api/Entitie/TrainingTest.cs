namespace TrainingManagement.Api.Entities;
public class TrainingTest
{
    public int TestId { get; set; }
    public int EmployeeId { get; set; }
    public int CourseId { get; set; }
    public string TestType { get; set; } = "";
    public int Score { get; set; }
    public DateTime TestDate { get; set; }
}
