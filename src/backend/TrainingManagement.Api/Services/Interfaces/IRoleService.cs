using TrainingManagement.Api.Dtos.Roles;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleResponse>> GetAllAsync(CancellationToken cancellationToken);
}
