using System.Data.Common;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

/// <summary>Oracle 连接 + 事务的最小会话实现。</summary>
public sealed class OracleDbSession : IDbSession
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;
    private bool _completed;

    /// <summary>接管已打开连接及其事务的生命周期。</summary>
    public OracleDbSession(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    /// <summary>返回仓储执行 SQL 时共用的连接。</summary>
    public DbConnection Connection => _connection;

    /// <summary>返回仓储命令必须绑定的同一事务。</summary>
    public DbTransaction Transaction => _transaction;

    /// <summary>仅提交一次；重复调用不会再次操作已完成事务。</summary>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_completed)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

    /// <summary>尝试回滚一次；回滚异常不会覆盖触发回滚的原始业务异常。</summary>
    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_completed)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception)
        {
            // 回滚失败不覆盖上层原始异常，连接随后会被释放。
        }
        finally
        {
            _completed = true;
        }
    }

    /// <summary>未显式提交的会话在释放时自动回滚，避免事务悬挂。</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await RollbackAsync(CancellationToken.None);
        }

        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
