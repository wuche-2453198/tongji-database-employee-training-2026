namespace TrainingManagement.Api.Dtos.Health;

/// <summary>应用自身健康信息，不作为数据库可用性的证明。</summary>
public sealed class SystemHealthResponse
{
    public string Status { get; init; } = "ok";

    public string Service { get; init; } = "TrainingManagement.Api";

    public string Environment { get; init; } = string.Empty;

    public string? Version { get; init; }

    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
}
