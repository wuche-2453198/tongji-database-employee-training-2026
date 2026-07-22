namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IHealthRepository
{
    bool IsDatabaseConfigured { get; }

    Task CheckDatabaseAsync(CancellationToken cancellationToken);
}
