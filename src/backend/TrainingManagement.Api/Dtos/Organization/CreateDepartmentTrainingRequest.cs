namespace TrainingManagement.Api.Dtos.Organization;

// 新增部门培训预算请求
public sealed class CreateDepartmentTrainingRequest
{
    // 部门名称（必填，唯一）
    public string DeptName { get; set; } = string.Empty;

    // 年度培训预算总额（必填，必须大于等于 0）
    public decimal AnnualBudget { get; set; }
}