using Dapper;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleAuthRepository : IAuthRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>通过依赖注入保存本类所需协作对象，供后续方法使用。</summary>
    public OracleAuthRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>按登录名、编号、邮箱、电话或姓名参数化查询员工，未匹配时返回空。</summary>
    public async Task<EmployeeAuthRecord?> FindEmployeeForLoginAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        var parsedEmpId = long.TryParse(identifier, out var empId)
            ? empId
            : (long?)null;

        const string sql = """
            SELECT *
            FROM (
                SELECT
                    e.EMP_ID AS "EmpId",
                    e.LOGIN_NAME AS "LoginName",
                    e.PASSWORD_HASH AS "PasswordHash",
                    e.EMP_NAME AS "EmpName",
                    d.DEPT_NAME AS "DeptName",
                    e.POSITION AS "Position",
                    e.EMAIL AS "Email",
                    e.PHONE AS "Phone",
                    e.STATUS AS "Status",
                    e.CREATED_AT AS "CreatedAt"
                FROM EMPLOYEES e
                LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
                WHERE (:EmpId IS NOT NULL AND e.EMP_ID = :EmpId)
                   OR LOWER(e.LOGIN_NAME) = LOWER(:Identifier)
                   OR LOWER(e.EMAIL) = LOWER(:Identifier)
                   OR e.PHONE = :Identifier
                   OR e.EMP_NAME = :Identifier
            )
            WHERE ROWNUM = 1
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<EmployeeAuthRecord>(
            new CommandDefinition(
                sql,
                new { EmpId = parsedEmpId, Identifier = identifier },
                cancellationToken: cancellationToken));
    }

    /// <summary>按真实员工主键读取认证所需资料及部门名称。</summary>
    public async Task<EmployeeAuthRecord?> GetEmployeeByIdAsync(
        long empId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                e.EMP_ID AS "EmpId",
                e.LOGIN_NAME AS "LoginName",
                e.PASSWORD_HASH AS "PasswordHash",
                e.EMP_NAME AS "EmpName",
                d.DEPT_NAME AS "DeptName",
                e.POSITION AS "Position",
                e.EMAIL AS "Email",
                e.PHONE AS "Phone",
                e.STATUS AS "Status",
                e.CREATED_AT AS "CreatedAt"
            FROM EMPLOYEES e
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
            WHERE e.EMP_ID = :EmpId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<EmployeeAuthRecord>(
            new CommandDefinition(
                sql,
                new { EmpId = empId },
                cancellationToken: cancellationToken));
    }

    /// <summary>通过用户角色关联表读取员工角色；当前表结构不存储权限列表，交给服务层映射。</summary>
    public async Task<IReadOnlyCollection<RoleRecord>> GetRolesByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken)
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
            FROM USER_ROLES ur
            INNER JOIN ROLES r ON r.ROLE_ID = ur.ROLE_ID
            WHERE ur.EMP_ID = :EmpId
            ORDER BY r.ROLE_ID
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<RoleRecord>(
            new CommandDefinition(
                sql,
                new { EmpId = empId },
                cancellationToken: cancellationToken));

        return roles.ToArray();
    }
}
