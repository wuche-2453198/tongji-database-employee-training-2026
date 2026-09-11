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

    public async Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId, DateTime testedAt)
    {
        const string sql = """
            INSERT INTO TRAINING_TESTS
                (EMP_ID, COURSE_ID, TEST_TYPE, SCORE, RECORDED_BY_EMP_ID, TESTED_AT)
            VALUES
                (:EmpId, :CourseId, :TestType, :Score, :RecordedByEmpId, :TestedAt)
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            request.EmpId,
            request.CourseId,
            request.TestType,
            request.Score,
            RecordedByEmpId = recordedByEmpId,
            TestedAt = testedAt
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
        int? employeeId, int? courseId, string? testType,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        if (employeeId.HasValue)
        {
            conditions.Add("t.EMP_ID = :EmpId");
            parameters.Add("EmpId", employeeId.Value);
        }
        if (courseId.HasValue)
        {
            conditions.Add("t.COURSE_ID = :CourseId");
            parameters.Add("CourseId", courseId.Value);
        }
        if (!string.IsNullOrEmpty(testType))
        {
            conditions.Add("t.TEST_TYPE = :TestType");
            parameters.Add("TestType", testType);
        }
        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            conditions.Add("e.EMP_NAME LIKE :EmployeeName");
            parameters.Add("EmployeeName", $"%{employeeName}%");
        }
        if (!string.IsNullOrWhiteSpace(courseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{courseName}%");
        }
        if (startDateFrom.HasValue)
        {
            conditions.Add("t.TESTED_AT >= :StartDateFrom");
            parameters.Add("StartDateFrom", startDateFrom.Value);
        }
        if (startDateTo.HasValue)
        {
            conditions.Add("t.TESTED_AT <= :StartDateTo");
            parameters.Add("StartDateTo", startDateTo.Value);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = """
            SELECT t.TEST_ID AS TestId, t.EMP_ID AS EmpId, t.COURSE_ID AS CourseId,
                   t.TEST_TYPE AS TestType, t.SCORE AS Score, t.TESTED_AT AS TestedAt,
                   t.RECORDED_BY_EMP_ID AS RecordedByEmpId,
                   e.EMP_NAME AS EmployeeName, c.COURSE_NAME AS CourseName
            FROM TRAINING_TESTS t
            LEFT JOIN EMPLOYEES e ON t.EMP_ID = e.EMP_ID
            LEFT JOIN TRAINING_COURSES c ON t.COURSE_ID = c.COURSE_ID
            """ + $"""

            {where}
            ORDER BY t.TESTED_AT DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;
        var countSql = $"""
            SELECT COUNT(1)
            FROM TRAINING_TESTS t
            LEFT JOIN EMPLOYEES e ON t.EMP_ID = e.EMP_ID
            LEFT JOIN TRAINING_COURSES c ON t.COURSE_ID = c.COURSE_ID
            {where}
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

    public async Task<string?> GetRegistrationStatusAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT STATUS FROM TRAINING_REGISTRATIONS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId
            ORDER BY REGISTERED_AT DESC
            FETCH FIRST 1 ROWS ONLY
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.ExecuteScalarAsync<string?>(sql, new { EmpId = employeeId, CourseId = courseId });
    }

    public async Task<PagedResult<TestScoreSummary>> GetScoreSummariesAsync(
        int? employeeId, int? courseId,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        if (employeeId.HasValue)
        {
            conditions.Add("t.EMP_ID = :EmpId");
            parameters.Add("EmpId", employeeId.Value);
        }
        if (courseId.HasValue)
        {
            conditions.Add("t.COURSE_ID = :CourseId");
            parameters.Add("CourseId", courseId.Value);
        }
        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            conditions.Add("e.EMP_NAME LIKE :EmployeeName");
            parameters.Add("EmployeeName", $"%{employeeName}%");
        }
        if (!string.IsNullOrWhiteSpace(courseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{courseName}%");
        }
        if (startDateFrom.HasValue)
        {
            conditions.Add("t.TESTED_AT >= :StartDateFrom");
            parameters.Add("StartDateFrom", startDateFrom.Value);
        }
        if (startDateTo.HasValue)
        {
            conditions.Add("t.TESTED_AT <= :StartDateTo");
            parameters.Add("StartDateTo", startDateTo.Value);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;

        // 按员工+课程聚合：PRE/POST 各取一条最高分，分数变化由 Oracle 计算(任一缺失即为 NULL)。
        var countSql = $"""
            SELECT COUNT(1) FROM (
                SELECT t.EMP_ID, t.COURSE_ID
                FROM TRAINING_TESTS t
                LEFT JOIN EMPLOYEES e ON t.EMP_ID = e.EMP_ID
                LEFT JOIN TRAINING_COURSES c ON t.COURSE_ID = c.COURSE_ID
                {where}
                GROUP BY t.EMP_ID, t.COURSE_ID
            )
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = $"""
            SELECT t.EMP_ID AS EmpId, e.EMP_NAME AS EmployeeName,
                   t.COURSE_ID AS CourseId, c.COURSE_NAME AS CourseName,
                   MAX(CASE WHEN t.TEST_TYPE = 'PRE' THEN t.SCORE END) AS PreScore,
                   MAX(CASE WHEN t.TEST_TYPE = 'POST' THEN t.SCORE END) AS PostScore,
                   MAX(CASE WHEN t.TEST_TYPE = 'POST' THEN t.SCORE END)
                       - MAX(CASE WHEN t.TEST_TYPE = 'PRE' THEN t.SCORE END) AS "Change",
                   MAX(t.TESTED_AT) AS UpdatedAt
            FROM TRAINING_TESTS t
            LEFT JOIN EMPLOYEES e ON t.EMP_ID = e.EMP_ID
            LEFT JOIN TRAINING_COURSES c ON t.COURSE_ID = c.COURSE_ID
            {where}
            GROUP BY t.EMP_ID, e.EMP_NAME, t.COURSE_ID, c.COURSE_NAME
            ORDER BY MAX(t.TESTED_AT) DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;

        var items = (await connection.QueryAsync<TestScoreSummary>(itemsSql, parameters)).ToArray();
        return new PagedResult<TestScoreSummary>(items, page, pageSize, total);
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
