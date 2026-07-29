namespace TrainingManagement.Api.Dtos.Health;

public sealed class DatabaseHealthResponse
{
    public string Status { get; init; } = string.Empty;

    public bool Connected { get; init; }

    public long ElapsedMilliseconds { get; init; }

    public string Provider { get; init; } = "Oracle.ManagedDataAccess.Core";

    public string? Message { get; init; }
}
