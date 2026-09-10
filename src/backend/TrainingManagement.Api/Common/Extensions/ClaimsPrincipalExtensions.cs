using System.Security.Claims;

namespace TrainingManagement.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>优先从标准身份声明读取员工编号，再兼容 emp_id；无法解析时返回空。</summary>
    public static long? GetEmployeeId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("emp_id");

        return long.TryParse(value, out var empId) ? empId : null;
    }
}
