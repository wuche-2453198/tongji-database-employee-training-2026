using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

/// <summary>统一管理普通 Oracle 连接和事务会话的创建规则。</summary>
public interface IDbConnectionFactory
{
    /// <summary>表示连接字符串是否已提供，不执行实际网络探测。</summary>
    bool IsConfigured { get; }

    /// <summary>打开 Oracle 连接并设置当前架构；返回的连接由调用方负责释放。</summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);

    /// <summary>打开连接并开启事务，用于需要跨模块同事务提交的写操作。</summary>
    Task<IDbSession> BeginSessionAsync(CancellationToken cancellationToken);
}
