using System.Data.Common;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Options;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

/// <summary>
/// Oracle 连接工厂：为每次仓储操作创建独立连接，并在连接打开后切换到业务 Schema。
/// Repository 使用 await using 释放连接，避免跨请求共享连接造成并发和生命周期问题。
/// </summary>
public sealed class OracleConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _currentSchema;
    private readonly int _connectionTimeoutSeconds;

    /// <summary>读取运行账号连接字符串和业务表所属 Schema，构造时不立即连接数据库。</summary>
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

    /// <summary>判断连接字符串是否已经配置；这里只检查非空，不代表数据库一定可达。</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>打开 Oracle 连接并设置当前架构；返回的连接由调用方负责释放。</summary>
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

    /// <summary>
    /// 创建带事务的数据库会话，供需要多条 SQL 原子提交的业务使用。
    /// 若事务创建失败，会立即释放已经打开的连接，避免连接池资源泄漏。
    /// </summary>
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
