namespace TrainingManagement.Api.Dtos.Registration;

public sealed class ActionEligibility
{
    public bool Allowed { get; set; }

    public string? Reason { get; set; }
}
