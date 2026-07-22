using System.Diagnostics;
using System.Reflection;
using TrainingManagement.Api.Dtos.Health;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class HealthService : IHealthService
{
    private readonly IHealthRepository _healthRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<HealthService> _logger;

    public HealthService(
        IHealthRepository healthRepository,
        IWebHostEnvironment environment,
        ILogger<HealthService> logger)
    {
        _healthRepository = healthRepository;
        _environment = environment;
        _logger = logger;
    }

    public SystemHealthResponse GetSystemHealth()
    {
        return new SystemHealthResponse
        {
            Environment = _environment.EnvironmentName,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            Time = DateTimeOffset.UtcNow
        };
    }

    public async Task<DatabaseHealthResponse> GetDatabaseHealthAsync(
        CancellationToken cancellationToken)
    {
        if (!_healthRepository.IsDatabaseConfigured)
        {
            return new DatabaseHealthResponse
            {
                Status = "not_configured",
                Connected = false,
                Message = "Oracle connection string is not configured."
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _healthRepository.CheckDatabaseAsync(cancellationToken);
            stopwatch.Stop();

            return new DatabaseHealthResponse
            {
                Status = "ok",
                Connected = true,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                exception,
                "Oracle health check failed. TraceId is available in the API response.");

            return new DatabaseHealthResponse
            {
                Status = "down",
                Connected = false,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                Message = "Oracle health check failed."
            };
        }
    }
}
