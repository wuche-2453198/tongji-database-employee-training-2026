using TrainingManagement.Api.Dtos.Health;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IHealthService
{
    SystemHealthResponse GetSystemHealth();

    Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken);
}
