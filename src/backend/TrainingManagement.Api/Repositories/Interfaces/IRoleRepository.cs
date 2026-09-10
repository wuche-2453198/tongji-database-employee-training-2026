using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IRoleRepository
{
    /// <summary>读取持久化角色记录，供服务层转换为响应。</summary>
    Task<IReadOnlyCollection<RoleRecord>> GetAllAsync(CancellationToken cancellationToken);
}
