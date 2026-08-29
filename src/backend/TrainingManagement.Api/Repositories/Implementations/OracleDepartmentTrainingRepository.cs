using Dapper;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

// 部门培训预算数据仓库实现
public sealed class OracleDepartmentTrainingRepository : IDepartmentTrainingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleDepartmentTrainingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<DepartmentTraining>> GetPagedAsync(
        DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new PagedResult<DepartmentTraining>
            {
                Items = Array.Empty<DepartmentTraining>(),
                Page = query.Page,
                PageSize = query.PageSize,
                Total = 0
            };
        }

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            conditions.Add("LOWER(d.DEPT_NAME) LIKE LOWER(:Keyword)");
            parameters.Add("Keyword", $"%{query.Keyword}%");
        }

        if (query.MinBudget.HasValue)
        {
            conditions.Add("d.ANNUAL_BUDGET >= :MinBudget");
            parameters.Add("MinBudget", query.MinBudget.Value);
        }

        if (query.MaxBudget.HasValue)
        {
            conditions.Add("d.ANNUAL_BUDGET <= :MaxBudget");
            parameters.Add("MaxBudget", query.MaxBudget.Value);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        // 排序
        var sortField = query.SortBy?.ToLower() switch
        {
            "deptname" => "d.DEPT_NAME",
            "annualbudget" => "d.ANNUAL_BUDGET",
            "usedbudget" => "d.USED_BUDGET",
            "remainbudget" => "d.REMAIN_BUDGET",
            _ => "d.DEPT_ID"
        };
        var sortOrder = query.SortOrder?.ToUpper() == "DESC" ? "DESC" : "ASC";

        var countSql = $@"
            SELECT COUNT(*)
            FROM DEPARTMENTS_TRAINING d
            {whereClause}";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var offset = (query.Page - 1) * query.PageSize;
        var dataSql = $@"
            SELECT
                d.DEPT_ID AS ""DeptId"",
                d.DEPT_NAME AS ""DeptName"",
                d.ANNUAL_BUDGET AS ""AnnualBudget"",
                d.USED_BUDGET AS ""UsedBudget"",
                d.REMAIN_BUDGET AS ""RemainBudget""
            FROM DEPARTMENTS_TRAINING d
            {whereClause}
            ORDER BY {sortField} {sortOrder}
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", query.PageSize);

        var items = await connection.QueryAsync<DepartmentTraining>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken));

        return new PagedResult<DepartmentTraining>
        {
            Items = items.ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            Total = total
        };
    }

    public async Task<DepartmentTraining?> GetByIdAsync(long deptId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = @"
            SELECT
                d.DEPT_ID AS ""DeptId"",
                d.DEPT_NAME AS ""DeptName"",
                d.ANNUAL_BUDGET AS ""AnnualBudget"",
                d.USED_BUDGET AS ""UsedBudget"",
                d.REMAIN_BUDGET AS ""RemainBudget""
            FROM DEPARTMENTS_TRAINING d
            WHERE d.DEPT_ID = :DeptId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<DepartmentTraining>(
            new CommandDefinition(sql, new { DeptId = deptId }, cancellationToken: cancellationToken));
    }

    public async Task<DepartmentTraining?> GetByNameAsync(string deptName, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = @"
            SELECT
                d.DEPT_ID AS ""DeptId"",
                d.DEPT_NAME AS ""DeptName"",
                d.ANNUAL_BUDGET AS ""AnnualBudget"",
                d.USED_BUDGET AS ""UsedBudget"",
                d.REMAIN_BUDGET AS ""RemainBudget""
            FROM DEPARTMENTS_TRAINING d
            WHERE LOWER(d.DEPT_NAME) = LOWER(:DeptName)";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<DepartmentTraining>(
            new CommandDefinition(sql, new { DeptName = deptName }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(long deptId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = "SELECT COUNT(1) FROM DEPARTMENTS_TRAINING WHERE DEPT_ID = :DeptId";
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { DeptId = deptId }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<bool> IsDeptNameExistsAsync(
        string deptName,
        long? excludeDeptId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        var sql = "SELECT COUNT(1) FROM DEPARTMENTS_TRAINING WHERE LOWER(DEPT_NAME) = LOWER(:DeptName)";
        var parameters = new DynamicParameters();
        parameters.Add("DeptName", deptName);

        if (excludeDeptId.HasValue)
        {
            sql += " AND DEPT_ID != :ExcludeDeptId";
            parameters.Add("ExcludeDeptId", excludeDeptId.Value);
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<DepartmentTraining> CreateAsync(
        DepartmentTraining entity,
        CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            throw new InvalidOperationException("Database connection is not configured.");
        }

        const string sql = @"
            INSERT INTO DEPARTMENTS_TRAINING (
                DEPT_ID,
                DEPT_NAME,
                ANNUAL_BUDGET,
                USED_BUDGET,
                REMAIN_BUDGET
            ) VALUES (
                SEQ_DEPARTMENTS_TRAINING.NEXTVAL,
                :DeptName,
                :AnnualBudget,
                0,
                :AnnualBudget
            )
            RETURNING DEPT_ID INTO :DeptId";

        var parameters = new DynamicParameters(entity);
        parameters.Add("DeptId", dbType: System.Data.DbType.Int64, direction: System.Data.ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var newDeptId = parameters.Get<long>("DeptId");
        entity.DeptId = newDeptId;
        entity.RemainBudget = entity.AnnualBudget; // 初始时剩余=年度预算
        return entity;
    }

    public async Task<bool> UpdateAsync(DepartmentTraining entity, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = @"
            UPDATE DEPARTMENTS_TRAINING SET
                DEPT_NAME = :DeptName,
                ANNUAL_BUDGET = :AnnualBudget,
                REMAIN_BUDGET = :AnnualBudget - USED_BUDGET
            WHERE DEPT_ID = :DeptId";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, entity, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long deptId, CancellationToken cancellationToken = default)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = "DELETE FROM DEPARTMENTS_TRAINING WHERE DEPT_ID = :DeptId";
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { DeptId = deptId }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}