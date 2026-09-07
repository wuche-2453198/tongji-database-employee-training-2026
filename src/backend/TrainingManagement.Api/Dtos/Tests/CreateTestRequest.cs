namespace TrainingManagement.Api.Dtos.Tests;
public class CreateTestRequest
{
    public int EmployeeId { get; set; }
    public int CourseId { get; set; }
    public string TestType { get; set; } = "";
    public int Score { get; set; }
}
