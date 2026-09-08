namespace TrainingManagement.Api.Dtos.Organization;

// 员工列表返回数据，不包含密码等敏感字段
public sealed class EmployeeResponse
{
    public long EmpId { get; set; }

    public string EmpName { get; set; } = string.Empty;

    public string DeptName { get; set; } = string.Empty;

    public string? Position { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime HireDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}