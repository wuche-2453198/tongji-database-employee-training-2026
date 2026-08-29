using Dapper;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

// 员工数据仓库实现
public sealed class OracleEmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleEmployeeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Employee>> GetPagedAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new PagedResult<Employee>
            {
                Items = Array.Empty<Employee>(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = 0
            };
        }

        // 构建查询条件
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            conditions.Add("(LOWER(e.EMP_NAME) LIKE LOWER(:Keyword) OR LOWER(e.LOGIN_NAME) LIKE LOWER(:Keyword) OR LOWER(e.EMAIL) LIKE LOWER(:Keyword))");
            parameters.Add("Keyword", $"%{query.Keyword}%");
        }

        if (!string.IsNullOrWhiteSpace(query.DeptName))
        {
            conditions.Add("e.DEPT_NAME = :DeptName");
            parameters.Add("DeptName", query.DeptName);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            conditions.Add("e.STATUS = :Status");
            parameters.Add("Status", query.Status);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        // 排序
        var sortField = query.SortBy?.ToLower() switch
        {
            "empname" => "e.EMP_NAME",
            "hiredate" => "e.HIRE_DATE",
            "createddate" => "e.CREATED_DATE",
            _ => "e.EMP_ID"
        };
        var sortOrder = query.SortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";

        // 总记录数
        var countSql = $@"
            SELECT COUNT(*)
            FROM EMPLOYEES e
            {whereClause}";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        // 分页查询
        var offset = (query.Page - 1) * query.PageSize;
        var dataSql = $@"
            SELECT
                e.EMP_ID AS ""EmpId"",
                e.LOGIN_NAME AS ""LoginName"",
                e.PASSWORD_HASH AS ""PasswordHash"",
                e.EMP_NAME AS ""EmpName"",
                e.DEPT_NAME AS ""DeptName"",
                e.POSITION AS ""Position"",
                e.EMAIL AS ""Email"",
                e.PHONE AS ""Phone"",
                e.HIRE_DATE AS ""HireDate"",
                e.STATUS AS ""Status"",
                e.CREATED_DATE AS ""CreatedDate""
            FROM EMPLOYEES e
            {whereClause}
            ORDER BY {sortField} {sortOrder}
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", query.PageSize);

        var items = await connection.QueryAsync<Employee>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

        return new PagedResult<Employee>
        {
            Items = items.ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    public async Task<Employee?> GetByIdAsync(long empId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = @"
            SELECT
                e.EMP_ID AS ""EmpId"",
                e.LOGIN_NAME AS ""LoginName"",
                e.PASSWORD_HASH AS ""PasswordHash"",
                e.EMP_NAME AS ""EmpName"",
                e.DEPT_NAME AS ""DeptName"",
                e.POSITION AS ""Position"",
                e.EMAIL AS ""Email"",
                e.PHONE AS ""Phone"",
                e.HIRE_DATE AS ""HireDate"",
                e.STATUS AS ""Status"",
                e.CREATED_DATE AS ""CreatedDate""
            FROM EMPLOYEES e
            WHERE e.EMP_ID = :EmpId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Employee>(
            new CommandDefinition(sql, new { EmpId = empId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(long empId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = "SELECT COUNT(1) FROM EMPLOYEES WHERE EMP_ID = :EmpId";
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EmpId = empId }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<bool> IsLoginNameExistsAsync(
        string loginName,
        long? excludeEmpId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        var sql = "SELECT COUNT(1) FROM EMPLOYEES WHERE LOWER(LOGIN_NAME) = LOWER(:LoginName)";
        var parameters = new DynamicParameters();
        parameters.Add("LoginName", loginName);

        if (excludeEmpId.HasValue)
        {
            sql += " AND EMP_ID != :ExcludeEmpId";
            parameters.Add("ExcludeEmpId", excludeEmpId.Value);
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            throw new InvalidOperationException("Database connection is not configured.");
        }

        const string sql = @"
            INSERT INTO EMPLOYEES (
                EMP_ID,
                LOGIN_NAME,
                PASSWORD_HASH,
                EMP_NAME,
                DEPT_NAME,
                POSITION,
                EMAIL,
                PHONE,
                HIRE_DATE,
                STATUS,
                CREATED_DATE
            ) VALUES (
                SEQ_EMPLOYEES.NEXTVAL,
                :LoginName,
                :PasswordHash,
                :EmpName,
                :DeptName,
                :Position,
                :Email,
                :Phone,
                :HireDate,
                :Status,
                :CreatedDate
            )
            RETURNING EMP_ID INTO :EmpId";

        var parameters = new DynamicParameters(employee);
        parameters.Add("EmpId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var newEmpId = parameters.Get<long>("EmpId");
        employee.EmpId = newEmpId;
        return employee;
    }

    public async Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = @"
            UPDATE EMPLOYEES SET
                EMP_NAME = :EmpName,
                DEPT_NAME = :DeptName,
                POSITION = :Position,
                EMAIL = :Email,
                PHONE = :Phone,
                HIRE_DATE = :HireDate,
                STATUS = :Status
            WHERE EMP_ID = :EmpId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, employee, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long empId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = @"
            UPDATE EMPLOYEES SET
                STATUS = 'RESIGNED'
            WHERE EMP_ID = :EmpId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { EmpId = empId }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}