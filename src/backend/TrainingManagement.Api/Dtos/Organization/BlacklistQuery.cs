namespace TrainingManagement.Api.Dtos.Organization;

// 黑名单查询参数
public sealed class BlacklistQuery
{
    // 员工编号
    public long? EmpId { get; set; }

    // 状态筛选：ACTIVE/RELEASED
    public string? Status { get; set; }

    // 部门名称筛选（主管仅能查看本部门时由服务端强制注入）
    public string? DeptName { get; set; }

    // 开始日期范围（起始）
    public DateTime? StartDateFrom { get; set; }

    // 开始日期范围（结束）
    public DateTime? StartDateTo { get; set; }

    // 页码
    public int Page { get; set; } = 1;

    // 每页大小
    public int PageSize { get; set; } = 20;

    // 排序字段：BlackId/EmpId/StartDate/EndDate/Status
    public string? SortBy { get; set; }

    // 排序方向：ASC/DESC
    public string? SortOrder { get; set; } = "ASC";
}