using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IDbConnectionFactory
{
    bool IsConfigured { get; }

    /// <summary>打开 Oracle 连接并设置当前架构；返回的连接由调用方负责释放。</summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
