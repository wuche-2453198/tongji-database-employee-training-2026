namespace TrainingManagement.Api.Entities;

public sealed class RegistrationSummaryRecord
{
    public long Total { get; init; }

    public long Registered { get; init; }

    public long SignedIn { get; init; }

    public long Absent { get; init; }

    public long Completed { get; init; }

    public long Canceled { get; init; }

    public int? MaxStudents { get; init; }
}
