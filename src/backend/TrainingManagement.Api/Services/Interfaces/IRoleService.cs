using TrainingManagement.Api.Dtos.Roles;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IRoleService
{
    /// <summary>获取对外展示的角色及权限列表。</summary>
    Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(CancellationToken cancellationToken);
}
