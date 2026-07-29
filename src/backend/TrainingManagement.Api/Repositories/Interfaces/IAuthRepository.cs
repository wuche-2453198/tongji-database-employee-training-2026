using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<EmployeeAuthRecord?> FindEmployeeForLoginAsync(
        string identifier,
        CancellationToken cancellationToken);

    Task<EmployeeAuthRecord?> GetEmployeeByIdAsync(
        long empId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleRecord>> GetRolesByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken);
}
