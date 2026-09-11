namespace TrainingManagement.Api.Common.Exceptions;

/// <summary>
/// 数据库未配置或连接无法建立。统一由异常中间件映射为 503，
/// 避免各模块在"连接串为空"与"连接串存在但库不可达"两种情况下分别返回 500 或空数据。
/// </summary>
public sealed class DatabaseUnavailableException : Exception
{
    /// <summary>保存面向客户端的提示，并保留底层 Oracle 异常供服务端日志诊断。</summary>
    public DatabaseUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
