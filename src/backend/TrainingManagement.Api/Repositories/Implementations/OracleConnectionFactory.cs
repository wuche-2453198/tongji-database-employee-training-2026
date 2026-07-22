using System.Data.Common;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _currentSchema;

    public OracleConnectionFactory(
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<OracleOptions> oracleOptions)
    {
        _connectionString = databaseOptions.Value.OracleDb;
        _currentSchema = oracleOptions.Value.CurrentSchema;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Oracle connection string is not configured.");
        }

        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await SetCurrentSchemaAsync(connection, cancellationToken);
        return connection;
    }

    private async Task SetCurrentSchemaAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentSchema))
        {
            return;
        }

        if (!IsSafeOracleIdentifier(_currentSchema))
        {
            throw new InvalidOperationException("Oracle current schema contains invalid characters.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER SESSION SET CURRENT_SCHEMA = {_currentSchema}";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsSafeOracleIdentifier(string value)
    {
        return value.Length <= 128
            && value.All(character =>
                char.IsAsciiLetterOrDigit(character)
                || character == '_'
                || character == '$'
                || character == '#');
    }
}
