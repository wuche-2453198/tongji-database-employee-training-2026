namespace TrainingManagement.Api.Dtos.Tests;

/// <summary>按员工+课程聚合的成绩汇总，供成绩列表展示。</summary>
public class TestScoreSummary
{
    public int EmpId { get; set; }

    public string? EmployeeName { get; set; }

    public int CourseId { get; set; }

    public string? CourseName { get; set; }

    /// <summary>训前成绩,未录入为 null。</summary>
    public decimal? PreScore { get; set; }

    /// <summary>训后成绩,未录入为 null。</summary>
    public decimal? PostScore { get; set; }

    /// <summary>分数变化 = POST - PRE;任一缺失为 null。</summary>
    public decimal? Change { get; set; }

    /// <summary>提升率(%) = (POST - PRE) / PRE * 100;PRE 缺失或为 0 时为 null。</summary>
    public decimal? ImprovementRate { get; set; }

    /// <summary>最近一次录入时间。</summary>
    public DateTime? UpdatedAt { get; set; }
}
