namespace TrainingManagement.Api.Dtos.Tests;

public class TestImprovementResponse
{
    /// <summary>PRE 成绩,未录入为 null。</summary>
    public decimal? PreScore { get; set; }

    /// <summary>POST 成绩,未录入为 null。</summary>
    public decimal? PostScore { get; set; }

    /// <summary>提升值 = POST - PRE;任一成绩缺失为 null。</summary>
    public decimal? Improvement { get; set; }

    /// <summary>提升率(%) = (POST - PRE) / PRE * 100;PRE 缺失或为 0 时为 null。</summary>
    public decimal? ImprovementRate { get; set; }
}
