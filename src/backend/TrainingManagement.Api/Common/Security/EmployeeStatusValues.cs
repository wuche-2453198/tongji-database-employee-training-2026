namespace TrainingManagement.Api.Common.Security;

public static class EmployeeStatusValues
{
    public const string Active = "ACTIVE";
    public const string Resigned = "RESIGNED";

    private const string ActiveCn = "\u5728\u804c";

    public static bool IsActive(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || string.Equals(status, Active, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ActiveCn, StringComparison.OrdinalIgnoreCase);
    }
}
