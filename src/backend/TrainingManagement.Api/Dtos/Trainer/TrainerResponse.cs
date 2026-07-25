namespace TrainingManagement.Api.Dtos.Trainer;

public sealed class TrainerResponse
{
    public long TrainerId { get; set; }

    public string TrainerName { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Company { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public decimal StarLevel { get; set; }

    public string IsInternal { get; set; } = "Y";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}