namespace TrainingManagement.Api.Dtos.Organization;

// 黑名单候选员工响应
public sealed class BlacklistCandidateResponse
{
    // 员工编号
    public long EmpId { get; set; }

    // 员工姓名
    public string EmpName { get; set; } = string.Empty;

    // 部门名称
    public string DeptName { get; set; } = string.Empty;

    // 职位
    public string? Position { get; set; }

    // 员工状态
    public string Status { get; set; } = string.Empty;
}
