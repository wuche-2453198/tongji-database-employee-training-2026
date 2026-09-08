namespace TrainingManagement.Api.Dtos.Organization;

// 员工查询参数
public sealed class EmployeeQuery
{
    // 关键词搜索
    public string? Keyword { get; set; }

    // 部门名称筛选
    public string? DeptName { get; set; }

    // 状态筛选：ACTIVE/RESIGNED
    public string? Status { get; set; }

    // 页码
    public int Page { get; set; } = 1;

    // 每页大小
    public int PageSize { get; set; } = 20;

    // 排序字段：EmpId/EmpName/HireDate/CreatedDate
    public string? SortBy { get; set; }

    // 排序方向：ASC/DESC
    public string? SortOrder { get; set; } = "ASC";
}