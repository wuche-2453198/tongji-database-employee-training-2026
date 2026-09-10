namespace TrainingManagement.Api.Common.Options;

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";

    public string CurrentSchema { get; init; } = string.Empty;

    /// <summary>
    /// Oracle 连接/连接池获取超时秒数。数据库不可达时必须早于前端请求超时（默认 10 秒）失败，
    /// 否则登录会先卡满客户端超时再回退演示账号，前端只会看到"登录服务暂时不可用"。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 5;
}
