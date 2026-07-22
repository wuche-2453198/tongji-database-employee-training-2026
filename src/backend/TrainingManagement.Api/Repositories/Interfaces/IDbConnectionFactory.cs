using System.Data.Common;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IDbConnectionFactory
{
    bool IsConfigured { get; }

    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
