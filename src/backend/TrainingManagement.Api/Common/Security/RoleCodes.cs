namespace TrainingManagement.Api.Common.Security;

public static class RoleCodes
{
    public const string Admin = "ADMIN";
    public const string Hr = "HR";
    public const string DepartmentManager = "DEPT_MANAGER";
    public const string Employee = "EMPLOYEE";

    private const string AdminCn = "\u7ba1\u7406\u5458";
    private const string DepartmentManagerCn = "\u90e8\u95e8\u4e3b\u7ba1";
    private const string EmployeeCn = "\u5458\u5de5";

    public static string Normalize(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Employee;
        }

        var value = roleName.Trim();

        if (string.Equals(value, Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, AdminCn, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        if (string.Equals(value, Hr, StringComparison.OrdinalIgnoreCase))
        {
            return Hr;
        }

        if (string.Equals(value, DepartmentManager, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "MANAGER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, DepartmentManagerCn, StringComparison.OrdinalIgnoreCase))
        {
            return DepartmentManager;
        }

        if (string.Equals(value, Employee, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, EmployeeCn, StringComparison.OrdinalIgnoreCase))
        {
            return Employee;
        }

        return value.ToUpperInvariant().Replace(' ', '_');
    }
}
