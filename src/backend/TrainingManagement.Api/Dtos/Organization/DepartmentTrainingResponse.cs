namespace TrainingManagement.Api.Dtos.Organization;

// 部门培训预算响应
public sealed class DepartmentTrainingResponse
{
    // 部门编号
    public long DeptId { get; set; }

    // 部门名称
    public string DeptName { get; set; } = string.Empty;

    // 年度培训预算总额
    public decimal AnnualBudget { get; set; }

    // 已使用预算
    public decimal UsedBudget { get; set; }

    // 剩余预算
    public decimal RemainBudget { get; set; }
}