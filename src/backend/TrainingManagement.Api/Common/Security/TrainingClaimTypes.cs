namespace TrainingManagement.Api.Common.Security;

/// <summary>JWT 中业务自定义声明的键名，签发和读取时保持一致。</summary>
public static class TrainingClaimTypes
{
    public const string EmployeeId = "emp_id";
    public const string Department = "dept_name";
    public const string Position = "position";
    public const string Permission = "permission";
}
