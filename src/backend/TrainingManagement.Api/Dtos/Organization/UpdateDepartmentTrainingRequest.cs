namespace TrainingManagement.Api.Dtos.Organization;

// 更新部门培训预算请求
public sealed class UpdateDepartmentTrainingRequest
{
    // 部门名称
    public string? DeptName { get; set; }

    // 年度培训预算总额
    public decimal? AnnualBudget { get; set; }
}