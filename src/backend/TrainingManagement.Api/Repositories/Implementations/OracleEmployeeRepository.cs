using Dapper;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Exceptions;
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
            conditions.Add("d.DEPT_NAME = :DeptName");
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
            "createdat" => "e.CREATED_AT",
            _ => "e.EMP_ID"
        };
        var sortOrder = query.SortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";

        // 总记录数
        var countSql = $@"
            SELECT COUNT(*)
            FROM EMPLOYEES e
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
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
                d.DEPT_NAME AS ""DeptName"",
                e.POSITION AS ""Position"",
                e.EMAIL AS ""Email"",
                e.PHONE AS ""Phone"",
                e.HIRE_DATE AS ""HireDate"",
                e.STATUS AS ""Status"",
                e.CREATED_AT AS ""CreatedAt""
            FROM EMPLOYEES e
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
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
                d.DEPT_NAME AS ""DeptName"",
                e.POSITION AS ""Position"",
                e.EMAIL AS ""Email"",
                e.PHONE AS ""Phone"",
                e.HIRE_DATE AS ""HireDate"",
                e.STATUS AS ""Status"",
                e.CREATED_AT AS ""CreatedAt""
            FROM EMPLOYEES e
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
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

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // 根据部门名称获取部门ID
        const string getDeptIdSql = "SELECT DEPT_ID FROM DEPARTMENTS_TRAINING WHERE DEPT_NAME = :DeptName";
        var deptId = await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(getDeptIdSql, new { DeptName = employee.DeptName }, cancellationToken: cancellationToken));

        if (!deptId.HasValue)
        {
            throw new BusinessException($"部门 '{employee.DeptName}' 不存在");
        }

        // 插入员工，使用DEPT_ID
        const string sql = @"
        INSERT INTO EMPLOYEES (
            LOGIN_NAME,
            PASSWORD_HASH,
            EMP_NAME,
            DEPT_ID,
            POSITION,
            EMAIL,
            PHONE,
            HIRE_DATE,
            STATUS,
            CREATED_AT
        ) VALUES (
            :LoginName,
            :PasswordHash,
            :EmpName,
            :DeptId,
            :Position,
            :Email,
            :Phone,
            :HireDate,
            :Status,
            :CreatedAt
        )
        RETURNING EMP_ID INTO :EmpId";

        var parameters = new DynamicParameters();
        parameters.Add("LoginName", employee.LoginName);
        parameters.Add("PasswordHash", employee.PasswordHash);
        parameters.Add("EmpName", employee.EmpName);
        parameters.Add("DeptId", deptId.Value);
        parameters.Add("Position", employee.Position);
        parameters.Add("Email", employee.Email);
        parameters.Add("Phone", employee.Phone);
        parameters.Add("HireDate", employee.HireDate);
        parameters.Add("Status", employee.Status);
        parameters.Add("CreatedAt", employee.CreatedAt);
        parameters.Add("EmpId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

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

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(employee.EmpName))
        {
            setClauses.Add("EMP_NAME = :EmpName");
            parameters.Add("EmpName", employee.EmpName);
        }

        if (!string.IsNullOrWhiteSpace(employee.DeptName))
        {
            // 部门名称转部门ID
            const string getDeptIdSql = "SELECT DEPT_ID FROM DEPARTMENTS_TRAINING WHERE DEPT_NAME = :DeptName";
            var deptId = await connection.ExecuteScalarAsync<long?>(
                new CommandDefinition(getDeptIdSql, new { DeptName = employee.DeptName }, cancellationToken: cancellationToken));
            if (!deptId.HasValue)
            {
                throw new BusinessException($"部门 '{employee.DeptName}' 不存在");
            }
            setClauses.Add("DEPT_ID = :DeptId");
            parameters.Add("DeptId", deptId.Value);
        }

        if (!string.IsNullOrWhiteSpace(employee.Position))
        {
            setClauses.Add("POSITION = :Position");
            parameters.Add("Position", employee.Position);
        }

        if (!string.IsNullOrWhiteSpace(employee.Email))
        {
            setClauses.Add("EMAIL = :Email");
            parameters.Add("Email", employee.Email);
        }

        if (!string.IsNullOrWhiteSpace(employee.Phone))
        {
            setClauses.Add("PHONE = :Phone");
            parameters.Add("Phone", employee.Phone);
        }

        if (employee.HireDate != default)
        {
            setClauses.Add("HIRE_DATE = :HireDate");
            parameters.Add("HireDate", employee.HireDate);
        }

        // 只有当Status不为空时才更新
        if (!string.IsNullOrWhiteSpace(employee.Status))
        {
            setClauses.Add("STATUS = :Status");
            parameters.Add("Status", employee.Status);
        }

        if (setClauses.Count == 0)
        {
            return true;
        }

        var sql = $"UPDATE EMPLOYEES SET {string.Join(", ", setClauses)} WHERE EMP_ID = :EmpId";
        parameters.Add("EmpId", employee.EmpId);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
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