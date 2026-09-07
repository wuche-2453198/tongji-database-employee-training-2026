using Dapper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;
public sealed class TestRepository : ITestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TestRepository(IDbConnectionFactory connectionFactory)
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

    public async Task<IEnumerable<TrainingTest>> GetListAsync(int? employeeId, int? courseId, string? testType)
    {
        var sql = """
            SELECT TEST_ID AS TestId, EMP_ID AS EmpId, COURSE_ID AS CourseId,
                   TEST_TYPE AS TestType, SCORE AS Score,
                   RECORDED_BY_EMP_ID AS RecordedByEmpId, TESTED_AT AS TestedAt
            FROM TRAINING_TESTS
            WHERE 1=1
            """;
        if (employeeId.HasValue) sql += " AND EMP_ID = :EmpId";
        if (courseId.HasValue) sql += " AND COURSE_ID = :CourseId";
        if (!string.IsNullOrEmpty(testType)) sql += " AND TEST_TYPE = :TestType";
        sql += " ORDER BY TEST_ID DESC";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryAsync<TrainingTest>(sql, new { EmpId = employeeId, CourseId = courseId, TestType = testType });
    }
}
