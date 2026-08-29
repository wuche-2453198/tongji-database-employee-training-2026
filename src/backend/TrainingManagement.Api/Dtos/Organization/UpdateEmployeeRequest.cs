namespace TrainingManagement.Api.Dtos.Organization;

// 更新员工请求
public sealed class UpdateEmployeeRequest
{
    // 员工姓名
    public string? EmpName { get; set; }

    // 部门名称
    public string? DeptName { get; set; }

    // 职位
    public string? Position { get; set; }

    // 邮箱
    public string? Email { get; set; }

    // 电话
    public string? Phone { get; set; }

    // 入职日期
    public DateTime? HireDate { get; set; }

    // 员工状态：ACTIVE/RESIGNED
    public string? Status { get; set; }
}