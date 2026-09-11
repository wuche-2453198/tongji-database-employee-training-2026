using TrainingManagement.Api.Dtos.Dashboard;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatsDto> GetStatsAsync(int employeeId, string role, CancellationToken cancellationToken);
}
