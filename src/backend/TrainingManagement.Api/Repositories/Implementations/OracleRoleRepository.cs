using Dapper;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleRoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleRoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<RoleRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return Array.Empty<RoleRecord>();
        }

        const string sql = """
            SELECT
                r.ROLE_ID AS "RoleId",
                r.ROLE_CODE AS "RoleCode",
                r.ROLE_NAME AS "RoleName",
                NULL AS "Permissions"
            FROM ROLES r
            ORDER BY r.ROLE_ID
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<RoleRecord>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return roles.ToArray();
    }
}
