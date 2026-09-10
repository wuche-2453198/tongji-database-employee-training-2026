using System.Data.Common;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _currentSchema;
    private readonly int _connectionTimeoutSeconds;

    public OracleConnectionFactory(
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<OracleOptions> oracleOptions)
    {
        _connectionString = databaseOptions.Value.OracleDb;
        _currentSchema = oracleOptions.Value.CurrentSchema;
        _connectionTimeoutSeconds = oracleOptions.Value.ConnectionTimeoutSeconds;

        if (IsConfigured && _connectionTimeoutSeconds > 0)
        {
            _connectionString = ApplyConnectionTimeout(_connectionString, _connectionTimeoutSeconds);
        }
    }

    /// <summary>
    /// Oracle 默认连接/连接池获取超时为 15 秒，超过前端 10 秒请求超时。
    /// 这里统一收紧为配置值，保证数据库不可达时能快速失败并走认证回退。
    /// </summary>
    private static string ApplyConnectionTimeout(string connectionString, int timeoutSeconds)
    {
        try
        {
            var builder = new OracleConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = timeoutSeconds
            };

            return builder.ConnectionString;
        }
        catch (Exception)
        {
            // 连接串无法解析时保持原样，由 Oracle 客户端在打开连接时给出明确错误。
            return connectionString;
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new DatabaseUnavailableException("数据库连接串未配置，服务暂不可用。");
        }

        var connection = new OracleConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await SetCurrentSchemaAsync(connection, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // 连接建立阶段的失败统一按"数据库不可达"处理（503），SQL 执行期错误仍为 500。
            await connection.DisposeAsync();
            throw new DatabaseUnavailableException(
                "数据库暂时不可用，请稍后重试。",
                exception);
        }

        return connection;
    }

    public async Task<IDbSession> BeginSessionAsync(CancellationToken cancellationToken)
    {
        var connection = await CreateOpenConnectionAsync(cancellationToken);

        try
        {
            var transaction = await connection.BeginTransactionAsync(cancellationToken);
            return new OracleDbSession(connection, transaction);
        }
        catch (Exception)
        {
            await connection.DisposeAsync();
            throw;
        }
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
