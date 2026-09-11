namespace TrainingManagement.Api.Common.Options;

/// <summary>Oracle 会话配置，指定业务表所属的默认架构。</summary>
public sealed class OracleOptions
{
    /// <summary>对应配置文件中的 Oracle 节点名称。</summary>
    public const string SectionName = "Oracle";

    /// <summary>运行账号连接后切换到的业务表所有者 Schema。</summary>
    public string CurrentSchema { get; init; } = string.Empty;

    /// <summary>
    /// Oracle 连接/连接池获取超时秒数。数据库不可达时必须早于前端请求超时（默认 10 秒）失败，
    /// 否则登录会先卡满客户端超时再回退演示账号，前端只会看到"登录服务暂时不可用"。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; init; } = 5;
}
