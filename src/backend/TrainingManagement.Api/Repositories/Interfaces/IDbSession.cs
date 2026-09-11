using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

/// <summary>
/// 跨模块同事务写入的最小会话。课程发布必须与部门预算占用共用同一个连接和事务，
/// 模块间只允许通过公开接口传递本会话，不允许互相引用对方的 Repository。
/// </summary>
public interface IDbSession : IAsyncDisposable
{
    /// <summary>供参与同一事务的仓储创建数据库命令。</summary>
    DbConnection Connection { get; }

    /// <summary>必须绑定到每条事务内命令的当前事务。</summary>
    DbTransaction Transaction { get; }

    /// <summary>提交会话内的全部数据库修改。</summary>
    Task CommitAsync(CancellationToken cancellationToken);

    /// <summary>撤销会话内尚未提交的数据库修改。</summary>
    Task RollbackAsync(CancellationToken cancellationToken);
}
