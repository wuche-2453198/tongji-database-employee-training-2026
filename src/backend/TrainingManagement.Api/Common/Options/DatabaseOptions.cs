namespace TrainingManagement.Api.Common.Options;

/// <summary>绑定连接字符串配置；实际凭据由环境变量或未提交的本地配置提供。</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string OracleDb { get; init; } = string.Empty;
}
