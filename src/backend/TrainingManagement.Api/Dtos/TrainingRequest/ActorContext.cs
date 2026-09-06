using TrainingManagement.Api.Common.Security;

namespace TrainingManagement.Api.Dtos.TrainingRequest;

/// <summary>
/// 当前请求者的身份上下文，由 Controller 从 JWT 声明构建。
/// </summary>
public sealed record ActorContext(int EmployeeId, IReadOnlyCollection<string> Roles)
{
    public bool IsAdmin => Roles.Contains(RoleCodes.Admin);

    public bool IsHr => Roles.Contains(RoleCodes.Hr);

    public bool IsManager => Roles.Contains(RoleCodes.DepartmentManager);
}
