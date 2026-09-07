using Dapper;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class TestRepository : ITestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(CreateTestRequest request)
    {
        const string sql = """
            INSERT INTO TRAINING_TESTS 
                (EMPLOYEE_ID, COURSE_ID, TEST_TYPE, SCORE, TEST_DATE) 
            VALUES 
                (:EmployeeId, :CourseId, :TestType, :Score, SYSDATE)
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            request.EmployeeId,
            request.CourseId,
            request.TestType,
            request.Score
        });

        return rows > 0;
    }

    public async Task<bool> UpdateScoreAsync(CreateTestRequest request)
    {
        const string sql = """
            UPDATE TRAINING_TESTS 
            SET SCORE = :Score, TEST_DATE = SYSDATE 
            WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId AND TEST_TYPE = :TestType
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            request.EmployeeId,
            request.CourseId,
            request.TestType,
            request.Score
        });

        return rows > 0;
    }

    public async Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_TESTS 
            WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId AND TEST_TYPE = :TestType
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, CourseId = courseId, TestType = testType });
        return count > 0;
    }

    public async Task<object> GetListAsync(int? employeeId, int? courseId, string? testType)
    {
        var sql = """
            SELECT * FROM TRAINING_TESTS 
            WHERE 1=1
            """;

        if (employeeId.HasValue) sql += " AND EMPLOYEE_ID = :EmployeeId";
        if (courseId.HasValue) sql += " AND COURSE_ID = :CourseId";
        if (!string.IsNullOrEmpty(testType)) sql += " AND TEST_TYPE = :TestType";
        sql += " ORDER BY TEST_ID DESC";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryAsync(sql, new { EmployeeId = employeeId, CourseId = courseId, TestType = testType });
    }
}
