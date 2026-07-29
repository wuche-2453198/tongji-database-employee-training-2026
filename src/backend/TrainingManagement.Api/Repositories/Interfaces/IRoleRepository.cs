using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<RoleRecord>> GetAllAsync(CancellationToken cancellationToken);
}
