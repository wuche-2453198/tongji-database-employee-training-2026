using Dapper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;
public sealed class OracleTestRepository : ITestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleTestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId)
    {
        const string sql = """
            INSERT INTO TRAINING_TESTS
                (EMP_ID, COURSE_ID, TEST_TYPE, SCORE, RECORDED_BY_EMP_ID, TESTED_AT)
            VALUES
                (:EmpId, :CourseId, :TestType, :Score, :RecordedByEmpId, SYSTIMESTAMP)
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            request.EmpId,
            request.CourseId,
            request.TestType,
            request.Score,
            RecordedByEmpId = recordedByEmpId
        });
        return rows > 0;
    }

    public async Task<bool> UpdateScoreAsync(CreateTestRequest request, int recordedByEmpId)
    {
        const string sql = """
            UPDATE TRAINING_TESTS
            SET SCORE = :Score, RECORDED_BY_EMP_ID = :RecordedByEmpId, TESTED_AT = SYSTIMESTAMP
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND TEST_TYPE = :TestType
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            request.EmpId,
            request.CourseId,
            request.TestType,
            request.Score,
            RecordedByEmpId = recordedByEmpId
        });
        return rows > 0;
    }

    public async Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_TESTS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND TEST_TYPE = :TestType
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, CourseId = courseId, TestType = testType });
        return count > 0;
    }

    public async Task<PagedResult<TrainingTest>> GetPagedListAsync(
        int? employeeId, int? courseId, string? testType, int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        if (employeeId.HasValue)
        {
            conditions.Add("EMP_ID = :EmpId");
            parameters.Add("EmpId", employeeId.Value);
        }
        if (courseId.HasValue)
        {
            conditions.Add("COURSE_ID = :CourseId");
            parameters.Add("CourseId", courseId.Value);
        }
        if (!string.IsNullOrEmpty(testType))
        {
            conditions.Add("TEST_TYPE = :TestType");
            parameters.Add("TestType", testType);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = """
            SELECT TEST_ID AS TestId, EMP_ID AS EmpId, COURSE_ID AS CourseId,
                   TEST_TYPE AS TestType, SCORE AS Score, TESTED_AT AS TestDate,
                   RECORDED_BY_EMP_ID AS RecordedByEmpId
            FROM TRAINING_TESTS
            """ + $"""

            {where}
            ORDER BY TEST_ID DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;
        var countSql = $"""
            SELECT COUNT(1) FROM TRAINING_TESTS {where}
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var items = (await connection.QueryAsync<TrainingTest>(itemsSql, parameters)).ToArray();
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        return new PagedResult<TrainingTest>(items, page, pageSize, total);
    }

    public async Task<ResultCourseGate> GetCourseGateAsync(int courseId)
    {
        const string sql = """
            SELECT POST_TEST_URL AS "PostTestUrl", START_AT AS "StartAt", END_AT AS "EndAt"
            FROM TRAINING_COURSES
            WHERE COURSE_ID = :CourseId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var gate = await connection.QueryFirstOrDefaultAsync<ResultCourseGate>(sql, new { CourseId = courseId });
        if (gate is null)
        {
            return new ResultCourseGate { Exists = false };
        }

        gate.Exists = true;
        return gate;
    }

    public async Task<bool> HasHrFiledRequestAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_REQUESTS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND STATUS = 'HR_FILED'
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, CourseId = courseId });
        return count > 0;
    }

    public async Task<(decimal? PreScore, decimal? PostScore)> GetScoresAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT TEST_TYPE, SCORE FROM TRAINING_TESTS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND TEST_TYPE IN ('PRE', 'POST')
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.QueryAsync<(string TestType, decimal Score)>(sql, new { EmpId = employeeId, CourseId = courseId });

        decimal? pre = null;
        decimal? post = null;
        foreach (var row in rows)
        {
            if (row.TestType == "PRE") pre = row.Score;
            if (row.TestType == "POST") post = row.Score;
        }

        return (pre, post);
    }
}
