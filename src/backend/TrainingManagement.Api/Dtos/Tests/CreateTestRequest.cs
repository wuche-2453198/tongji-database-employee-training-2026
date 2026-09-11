namespace TrainingManagement.Api.Dtos.Tests;
public class CreateTestRequest
{
    public int EmpId { get; set; }

    public int CourseId { get; set; }

    /// <summary>测试类型，只能是 PRE 或 POST。</summary>
    public string TestType { get; set; } = "";

    /// <summary>成绩，取值 0～100，允许一位以内小数。</summary>
    public decimal Score { get; set; }

    /// <summary>测试时间，缺省为服务端当前时间。</summary>
    public DateTime? TestedAt { get; set; }
}
