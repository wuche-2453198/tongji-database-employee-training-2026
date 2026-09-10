using TrainingManagement.Api.Dtos.Health;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IHealthService
{
    /// <summary>返回应用环境、程序集版本及当前时间，不访问数据库。</summary>
    SystemHealthResponse GetSystemHealth();

    /// <summary>执行数据库探测并计时，区分未配置、连接成功和探测失败。</summary>
    Task<DatabaseHealthResponse> GetDatabaseHealthAsync(CancellationToken cancellationToken);
}
