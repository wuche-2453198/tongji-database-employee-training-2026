using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Dashboard;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public Task<DashboardStatsDto> GetStatsAsync(ActorContext actor, CancellationToken cancellationToken)
    {
        var role = actor.IsAdmin
            ? RoleCodes.Admin
            : actor.IsHr
                ? RoleCodes.Hr
                : actor.IsManager
                    ? RoleCodes.DepartmentManager
                    : RoleCodes.Employee;

        return _repository.GetStatsAsync(actor.EmployeeId, role, cancellationToken);
    }
}
