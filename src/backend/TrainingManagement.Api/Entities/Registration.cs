namespace TrainingManagement.Api.Entities;
public class Registration
{
    public int RegId { get; set; }
    public int EmpId { get; set; }
    public int CourseId { get; set; }
    public string Status { get; set; } = "";
}
