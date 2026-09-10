namespace TrainingManagement.Api.Common.Options;

/// <summary>Oracle 会话配置，指定业务表所属的默认架构。</summary>
public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string CurrentSchema { get; init; } = string.Empty;
}
