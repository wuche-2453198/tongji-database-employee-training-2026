namespace TrainingManagement.Api.Common.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string OracleDb { get; init; } = string.Empty;
}
