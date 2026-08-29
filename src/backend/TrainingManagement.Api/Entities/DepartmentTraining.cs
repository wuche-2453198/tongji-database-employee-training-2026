namespace TrainingManagement.Api.Entities;

// 部门培训预算实体，对应数据库DEPARTMENTS_TRAINING表
public sealed class DepartmentTraining
{
    // 部门编号（主键，序列生成）
    public long DeptId { get; set; }

    // 部门名称（唯一约束）
    public string DeptName { get; set; } = string.Empty;

    // 年度培训预算总额
    public decimal AnnualBudget { get; set; }

    // 已使用预算
    public decimal UsedBudget { get; set; }

    // 剩余预算
    public decimal RemainBudget { get; set; }
}