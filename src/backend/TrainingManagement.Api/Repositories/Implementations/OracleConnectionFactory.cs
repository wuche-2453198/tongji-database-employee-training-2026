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

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public OracleConnectionFactory(
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<OracleOptions> oracleOptions)
    {
        _connectionString = databaseOptions.Value.OracleDb;
        _currentSchema = oracleOptions.Value.CurrentSchema;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>打开 Oracle 连接并设置当前架构；返回的连接由调用方负责释放。</summary>
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

    /// <summary>设置当前会话的默认架构，使无表前缀 SQL 能访问业务表；该设置不会授予额外权限。</summary>
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

    /// <summary>限定拼接进架构名的字符和长度，阻止引号、空格等进入会话 SQL。</summary>
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
