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

        if (query.StartDateFrom.HasValue)
        {
            conditions.Add("b.START_DATE >= :StartDateFrom");
            parameters.Add("StartDateFrom", query.StartDateFrom.Value);
        }

        if (query.StartDateTo.HasValue)
        {
            conditions.Add("b.START_DATE <= :StartDateTo");
            parameters.Add("StartDateTo", query.StartDateTo.Value);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sortField = query.SortBy?.ToLower() switch
        {
            "empid" => "b.EMP_ID",
            "startdate" => "b.START_DATE",
            "enddate" => "b.END_DATE",
            "status" => "b.STATUS",
            _ => "b.BLACK_ID"
        };
        var sortOrder = query.SortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";

        var countSql = $@"
            SELECT COUNT(*)
            FROM BLACKLIST b
            {whereClause}";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var offset = (query.Page - 1) * query.PageSize;
        var dataSql = $@"
            SELECT
                b.BLACK_ID AS ""BlackId"",
                b.EMP_ID AS ""EmpId"",
                b.REASON AS ""Reason"",
                b.START_DATE AS ""StartDate"",
                b.END_DATE AS ""EndDate"",
                b.STATUS AS ""Status""
            FROM BLACKLIST b
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
                b.REASON AS ""Reason"",
                b.START_DATE AS ""StartDate"",
                b.END_DATE AS ""EndDate"",
                b.STATUS AS ""Status""
            FROM BLACKLIST b
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
                b.START_DATE AS ""StartDate"",
                b.END_DATE AS ""EndDate"",
                b.STATUS AS ""Status""
            FROM BLACKLIST b
            WHERE b.EMP_ID = :EmpId
              AND b.STATUS = 'ACTIVE'
              AND (b.END_DATE IS NULL OR b.END_DATE >= SYSDATE)
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
              AND STATUS = 'ACTIVE'
              AND (END_DATE IS NULL OR END_DATE >= SYSDATE)";

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
                BLACK_ID,
                EMP_ID,
                REASON,
                START_DATE,
                END_DATE,
                STATUS
            ) VALUES (
                SEQ_BLACKLIST.NEXTVAL,
                :EmpId,
                :Reason,
                :StartDate,
                :EndDate,
                :Status
            )
            RETURNING BLACK_ID INTO :BlackId";

        var parameters = new DynamicParameters(entity);
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
                END_DATE = :EndDate,
                STATUS = :Status
            WHERE BLACK_ID = :BlackId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
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