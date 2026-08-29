namespace TrainingManagement.Api.Dtos.Organization;

// 部门培训预算查询参数
public sealed class DepartmentTrainingQuery
{
    // 关键词搜索（部门名称）
    public string? Keyword { get; set; }

    // 年度预算最小值
    public decimal? MinBudget { get; set; }

    // 年度预算最大值
    public decimal? MaxBudget { get; set; }

    // 页码
    public int Page { get; set; } = 1;

    // 每页大小
    public int PageSize { get; set; } = 20;

    // 排序字段：DeptId/DeptName/AnnualBudget/UsedBudget/RemainBudget
    public string? SortBy { get; set; }

    // 排序方向：ASC/DESC
    public string? SortOrder { get; set; } = "ASC";
}