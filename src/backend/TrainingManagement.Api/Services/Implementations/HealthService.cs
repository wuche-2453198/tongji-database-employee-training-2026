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

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public HealthService(
        IHealthRepository healthRepository,
        IWebHostEnvironment environment,
        ILogger<HealthService> logger)
    {
        _healthRepository = healthRepository;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>返回应用环境、程序集版本及当前时间，不访问数据库。</summary>
    public SystemHealthResponse GetSystemHealth()
    {
        return new SystemHealthResponse
        {
            Environment = _environment.EnvironmentName,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            Time = DateTimeOffset.UtcNow
        };
    }

    /// <summary>执行数据库探测并计时，区分未配置、连接成功和探测失败。</summary>
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
