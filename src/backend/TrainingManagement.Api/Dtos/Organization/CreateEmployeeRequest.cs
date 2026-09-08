namespace TrainingManagement.Api.Dtos.Organization;

// 新增员工请求
public sealed class CreateEmployeeRequest
{
    // 登录名
    public string LoginName { get; set; } = string.Empty;

    // 员工姓名
    public string EmpName { get; set; } = string.Empty;

    // 部门名称
    public string DeptName { get; set; } = string.Empty;

    // 职位
    public string? Position { get; set; }

    // 邮箱
    public string? Email { get; set; }

    // 电话
    public string? Phone { get; set; }

    // 入职日期
    public DateTime HireDate { get; set; }

    // 密码
    public string Password { get; set; } = string.Empty;
}