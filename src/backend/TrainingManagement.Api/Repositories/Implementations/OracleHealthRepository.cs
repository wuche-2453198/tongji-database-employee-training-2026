using Dapper;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleHealthRepository : IHealthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public OracleHealthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public bool IsDatabaseConfigured => _connectionFactory.IsConfigured;

    /// <summary>执行 SELECT 1 FROM DUAL 验证数据库连接和基本查询能力，不代表所有业务表均可访问。</summary>
    public async Task CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT 1 FROM DUAL", cancellationToken: cancellationToken));
    }
}
