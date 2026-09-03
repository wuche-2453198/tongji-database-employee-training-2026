namespace TrainingManagement.Api.Dtos.Registration;

public sealed class RegistrationSummaryResponse
{
    public long Total { get; set; }

    public long Registered { get; set; }

    public long SignedIn { get; set; }

    public long Absent { get; set; }

    public long Completed { get; set; }

    public long Canceled { get; set; }

    public long? RemainingSeats { get; set; }
}
