namespace TrainingManagement.Api.Dtos.Health;

public sealed class SystemHealthResponse
{
    public string Status { get; init; } = "ok";

    public string Service { get; init; } = "TrainingManagement.Api";

    public string Environment { get; init; } = string.Empty;

    public string? Version { get; init; }

    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;
}
