using Dapper;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleHealthRepository : IHealthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleHealthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool IsDatabaseConfigured => _connectionFactory.IsConfigured;

    public async Task CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT 1 FROM DUAL", cancellationToken: cancellationToken));
    }
}
