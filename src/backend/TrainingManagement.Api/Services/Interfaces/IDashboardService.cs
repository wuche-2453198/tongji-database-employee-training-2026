using TrainingManagement.Api.Dtos.Dashboard;
using TrainingManagement.Api.Dtos.TrainingRequest;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(ActorContext actor, CancellationToken cancellationToken);
}
