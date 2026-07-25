namespace TrainingManagement.Api.Entities;

public sealed class Trainer
{
    public long TrainerId { get; init; }

    public string TrainerName { get; init; } = string.Empty;

    public string? Title { get; init; }

    public string? Company { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public decimal StarLevel { get; init; }

    public string IsInternal { get; init; } = "Y";

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }
}