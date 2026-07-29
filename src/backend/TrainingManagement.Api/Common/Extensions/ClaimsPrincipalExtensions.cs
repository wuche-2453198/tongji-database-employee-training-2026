using System.Security.Claims;

namespace TrainingManagement.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long? GetEmployeeId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("emp_id");

        return long.TryParse(value, out var empId) ? empId : null;
    }
}
