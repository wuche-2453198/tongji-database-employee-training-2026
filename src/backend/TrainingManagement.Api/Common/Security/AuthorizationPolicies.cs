namespace TrainingManagement.Api.Common.Security;

/// <summary>服务端角色授权策略名称，具体角色要求在 Program 中注册。</summary>
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string HrOrAdmin = "HrOrAdmin";
    public const string ManagerHrOrAdmin = "ManagerHrOrAdmin";
}
