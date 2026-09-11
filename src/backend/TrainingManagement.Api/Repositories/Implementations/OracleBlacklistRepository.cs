using Dapper;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

// 黑名单数据仓库实现
public sealed class OracleBlacklistRepository : IBlacklistRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleBlacklistRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Blacklist>> GetPagedAsync(
        BlacklistQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new PagedResult<Blacklist>
            {
                Items = Array.Empty<Blacklist>(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = 0
            };
        }

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (query.EmpId.HasValue)
        {
            conditions.Add("b.EMP_ID = :EmpId");
            parameters.Add("EmpId", query.EmpId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            conditions.Add("b.STATUS = :Status");
            parameters.Add("Status", query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.DeptName))
        {
            conditions.Add("d.DEPT_NAME = :DeptName");
            parameters.Add("DeptName", query.DeptName);
        }

        if (query.StartDateFrom.HasValue)
        {
            conditions.Add("b.START_AT >= :StartDateFrom");
            parameters.Add("StartDateFrom", query.StartDateFrom.Value);
        }

        if (query.StartDateTo.HasValue)
        {
            conditions.Add("b.START_AT <= :StartDateTo");
            parameters.Add("StartDateTo", query.StartDateTo.Value);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sortField = query.SortBy?.ToLower() switch
        {
            "empid" => "b.EMP_ID",
            "startdate" => "b.START_AT",
            "enddate" => "b.END_AT",
            "status" => "b.STATUS",
            _ => "b.BLACK_ID"
        };
        var sortOrder = query.SortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";

        var countSql = $@"
            SELECT COUNT(*)
            FROM BLACKLIST b
            LEFT JOIN EMPLOYEES e ON e.EMP_ID = b.EMP_ID
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
            {whereClause}";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var offset = (query.Page - 1) * query.PageSize;
        var dataSql = $@"
            SELECT
                b.BLACK_ID AS ""BlackId"",
                b.EMP_ID AS ""EmpId"",
                e.EMP_NAME AS ""EmpName"",
                d.DEPT_NAME AS ""DeptName"",
                b.REASON AS ""Reason"",
                b.START_AT AS ""StartDate"",
                b.END_AT AS ""EndDate"",
                b.STATUS AS ""Status"",
                b.CREATED_AT AS ""CreatedAt""
            FROM BLACKLIST b
            LEFT JOIN EMPLOYEES e ON e.EMP_ID = b.EMP_ID
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
            {whereClause}
            ORDER BY {sortField} {sortOrder}
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", query.PageSize);

        var items = await connection.QueryAsync<Blacklist>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

        return new PagedResult<Blacklist>
        {
            Items = items.ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    public async Task<Blacklist?> GetByIdAsync(long blackId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = @"
            SELECT
                b.BLACK_ID AS ""BlackId"",
                b.EMP_ID AS ""EmpId"",
                e.EMP_NAME AS ""EmpName"",
                d.DEPT_NAME AS ""DeptName"",
                b.REASON AS ""Reason"",
                b.START_AT AS ""StartDate"",
                b.END_AT AS ""EndDate"",
                b.STATUS AS ""Status"",
                b.CREATED_AT AS ""CreatedAt""
            FROM BLACKLIST b
            LEFT JOIN EMPLOYEES e ON e.EMP_ID = b.EMP_ID
            LEFT JOIN DEPARTMENTS_TRAINING d ON d.DEPT_ID = e.DEPT_ID
            WHERE b.BLACK_ID = :BlackId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Blacklist>(
            new CommandDefinition(sql, new { BlackId = blackId }, cancellationToken: cancellationToken));
    }

    public async Task<Blacklist?> GetActiveByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = @"
            SELECT
                b.BLACK_ID AS ""BlackId"",
                b.EMP_ID AS ""EmpId"",
                b.REASON AS ""Reason"",
                b.START_AT AS ""StartDate"",
                b.END_AT AS ""EndDate"",
                b.STATUS AS ""Status""
            FROM BLACKLIST b
            WHERE b.EMP_ID = :EmpId
              AND b.STATUS = 'ACTIVE'
              AND (b.END_AT IS NULL OR b.END_AT >= SYSDATE)
            ORDER BY b.BLACK_ID
            FETCH FIRST 1 ROWS ONLY";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Blacklist>(
            new CommandDefinition(sql, new { EmpId = empId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> IsEmployeeBlacklistedAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = @"
            SELECT COUNT(1)
            FROM BLACKLIST
            WHERE EMP_ID = :EmpId
              AND STATUS = 'ACTIVE'";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EmpId = empId }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<bool> ExistsAsync(long blackId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = "SELECT COUNT(1) FROM BLACKLIST WHERE BLACK_ID = :BlackId";
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { BlackId = blackId }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<Blacklist> CreateAsync(Blacklist entity, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            throw new InvalidOperationException("Database connection is not configured.");
        }

        const string sql = @"
            INSERT INTO BLACKLIST (
                EMP_ID,
                REASON,
                START_AT,
                END_AT,
                STATUS,
                CREATED_AT,
                UPDATED_AT
            ) VALUES (
                :EmpId,
                :Reason,
                :StartDate,
                :EndDate,
                :Status,
                :CreatedAt,
                :UpdatedAt
            )
            RETURNING BLACK_ID INTO :BlackId";

        var parameters = new DynamicParameters();
        parameters.Add("EmpId", entity.EmpId);
        parameters.Add("Reason", entity.Reason);
        parameters.Add("StartDate", entity.StartDate);
        parameters.Add("EndDate", entity.EndDate);
        parameters.Add("Status", entity.Status);
        parameters.Add("CreatedAt", entity.CreatedAt);
        parameters.Add("UpdatedAt", entity.CreatedAt);
        parameters.Add("BlackId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var newBlackId = parameters.Get<long>("BlackId");
        entity.BlackId = newBlackId;
        return entity;
    }

    public async Task<bool> UpdateAsync(Blacklist entity, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = @"
            UPDATE BLACKLIST SET
                REASON = :Reason,
                END_AT = :EndDate,
                STATUS = :Status,
                UPDATED_AT = :UpdatedAt
            WHERE BLACK_ID = :BlackId";

        var parameters = new DynamicParameters();
        parameters.Add("BlackId", entity.BlackId);
        parameters.Add("Reason", entity.Reason);
        parameters.Add("EndDate", entity.EndDate);
        parameters.Add("Status", entity.Status);
        parameters.Add("UpdatedAt", DateTime.Now);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long blackId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = "DELETE FROM BLACKLIST WHERE BLACK_ID = :BlackId";
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { BlackId = blackId }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}