namespace TrainingManagement.Api.Dtos.Organization;

// 黑名单候选员工查询参数
// 候选名单用于「加入黑名单」时选择员工，
// 已排除管理员、HR、部门主管等具备管理权限的账号。
public sealed class BlacklistCandidateQuery
{
    // 姓名/工号/登录名关键词
    public string? Keyword { get; set; }

    // 部门名称筛选（部门主管仅能查看本部门时由服务端强制注入）
    public string? DeptName { get; set; }

    // 页码
    public int Page { get; set; } = 1;

    // 每页大小
    public int PageSize { get; set; } = 50;
}
