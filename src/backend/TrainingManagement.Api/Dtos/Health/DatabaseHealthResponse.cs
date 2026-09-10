namespace TrainingManagement.Api.Dtos.Health;

/// <summary>数据库探测结果，包含连接状态和耗时。</summary>
public sealed class DatabaseHealthResponse
{
    public string Status { get; init; } = string.Empty;

    public bool Connected { get; init; }

    public long ElapsedMilliseconds { get; init; }

    public string Provider { get; init; } = "Oracle.ManagedDataAccess.Core";

    public string? Message { get; init; }
}
