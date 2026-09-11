using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IDbConnectionFactory
{
    bool IsConfigured { get; }

    /// <summary>打开 Oracle 连接并设置当前架构；返回的连接由调用方负责释放。</summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);

    /// <summary>打开连接并开启事务，用于需要跨模块同事务提交的写操作。</summary>
    Task<IDbSession> BeginSessionAsync(CancellationToken cancellationToken);
}
