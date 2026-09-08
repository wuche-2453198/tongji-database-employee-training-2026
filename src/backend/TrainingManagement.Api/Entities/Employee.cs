using System;

namespace TrainingManagement.Api.Entities;

public sealed class Employee
{
	// 员工编号，主键
    public long EmpId { get; set; }

	// 登录名
    public string LoginName { get; set; } = string.Empty;

    // 密码哈希
    public string PasswordHash { get; set; } = string.Empty;

    // 员工姓名
    public string EmpName { get; set; } = string.Empty;

    // 部门名称，外键，关联DepartmentsTraining.DeptName
    public string DeptName { get; set; } = string.Empty;

    // 职位
    public string? Position { get; set; }

    // 邮箱
    public string? Email { get; set; }

    // 电话
    public string? Phone { get; set; }

    // 入职日期
    public DateTime HireDate { get; set; }

    // 员工状态
    public string Status { get; set; } = "ACTIVE";

    // 创建日期
    public DateTime CreatedAt { get; set; }
}
