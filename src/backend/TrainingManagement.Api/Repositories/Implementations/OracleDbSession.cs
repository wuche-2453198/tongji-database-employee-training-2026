using System.Data.Common;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

/// <summary>Oracle 连接 + 事务的最小会话实现。</summary>
public sealed class OracleDbSession : IDbSession
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;
    private bool _completed;

    public OracleDbSession(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public DbConnection Connection => _connection;

    public DbTransaction Transaction => _transaction;

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_completed)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken);
        _completed = true;
    }

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
