namespace TrainingManagement.Api.Dtos.Tests;
public class CreateTestRequest
{
    public int EmpId { get; set; }
    public int CourseId { get; set; }
    public string TestType { get; set; } = "";
    public int Score { get; set; }
}
