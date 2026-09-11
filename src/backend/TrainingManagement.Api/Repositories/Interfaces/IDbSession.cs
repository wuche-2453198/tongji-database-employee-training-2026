using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

/// <summary>
/// 跨模块同事务写入的最小会话。课程发布必须与部门预算占用共用同一个连接和事务，
/// 模块间只允许通过公开接口传递本会话，不允许互相引用对方的 Repository。
/// </summary>
public interface IDbSession : IAsyncDisposable
{
    DbConnection Connection { get; }

    DbTransaction Transaction { get; }

    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
