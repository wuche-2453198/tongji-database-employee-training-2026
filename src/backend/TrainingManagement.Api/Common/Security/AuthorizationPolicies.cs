namespace TrainingManagement.Api.Common.Security;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string HrOrAdmin = "HrOrAdmin";
    public const string ManagerHrOrAdmin = "ManagerHrOrAdmin";
    public const string AdminOrManager = "AdminOrManager";
}
