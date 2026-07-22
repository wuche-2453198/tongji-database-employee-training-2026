namespace TrainingManagement.Api.Common.Options;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string CurrentSchema { get; init; } = string.Empty;
}
