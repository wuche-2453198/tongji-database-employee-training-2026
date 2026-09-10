using Dapper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;
public sealed class OracleRatingRepository : IRatingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleRatingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(CreateRatingRequest request, int employeeId)
    {
        const string sql = """
            INSERT INTO TRAINER_RATINGS
                (EMP_ID, COURSE_ID, TRAINER_ID, SCORE, RATING_COMMENT, RATED_AT)
            VALUES
                (:EmpId, :CourseId, :TrainerId, :Score, :RatingComment, SYSTIMESTAMP)
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            EmpId = employeeId,
            request.CourseId,
            request.TrainerId,
            request.Score,
            request.RatingComment
        });
        return rows > 0;
    }

    public async Task<(IReadOnlyList<TrainerRating> Items, int Total)> GetListAsync(
        int? courseId, int? trainerId, int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();
        if (courseId.HasValue)
        {
            conditions.Add("COURSE_ID = :CourseId");
            parameters.Add("CourseId", courseId.Value);
        }
        if (trainerId.HasValue)
        {
            conditions.Add("TRAINER_ID = :TrainerId");
            parameters.Add("TrainerId", trainerId.Value);
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = """
            SELECT RATING_ID AS RatingId, COURSE_ID AS CourseId, TRAINER_ID AS TrainerId,
                   EMP_ID AS EmpId, SCORE AS Score, RATING_COMMENT AS RatingComment,
                   HR_VERIFIED AS HrVerified, HR_VERIFIER_EMP_ID AS HrVerifierEmpId,
                   VERIFIED_AT AS VerifiedAt, HR_COMMENT AS HrComment, RATED_AT AS RatedAt
            FROM TRAINER_RATINGS
            """ + $"""

            {where}
            ORDER BY RATING_ID DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;
        var countSql = $"""
            SELECT COUNT(1) FROM TRAINER_RATINGS {where}
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var items = (await connection.QueryAsync<TrainerRating>(itemsSql, parameters)).ToArray();
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        return (items, total);
    }

    public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINER_RATINGS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, CourseId = courseId });
        return count > 0;
    }

    public async Task<TrainerRating?> GetByIdAsync(int ratingId)
    {
        const string sql = """
            SELECT RATING_ID AS RatingId, COURSE_ID AS CourseId, TRAINER_ID AS TrainerId,
                   EMP_ID AS EmpId, SCORE AS Score, RATING_COMMENT AS RatingComment,
                   HR_VERIFIED AS HrVerified, HR_VERIFIER_EMP_ID AS HrVerifierEmpId,
                   VERIFIED_AT AS VerifiedAt, HR_COMMENT AS HrComment, RATED_AT AS RatedAt
            FROM TRAINER_RATINGS
            WHERE RATING_ID = :RatingId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryFirstOrDefaultAsync<TrainerRating>(sql, new { RatingId = ratingId });
    }

    public async Task<bool> VerifyAsync(int ratingId, string verifyComment, int hrVerifierEmpId)
    {
        const string sql = """
            UPDATE TRAINER_RATINGS
            SET HR_VERIFIED = 'Y', HR_VERIFIER_EMP_ID = :HrVerifierEmpId,
                VERIFIED_AT = SYSTIMESTAMP, HR_COMMENT = :HrComment
            WHERE RATING_ID = :RatingId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new
        {
            RatingId = ratingId,
            HrVerifierEmpId = hrVerifierEmpId,
            HrComment = verifyComment
        });
        return rows > 0;
    }

    public async Task<bool> HasCompletedRegistrationAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_REGISTRATIONS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND STATUS = 'COMPLETED'
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, CourseId = courseId });
        return count > 0;
    }

    public async Task<(decimal? AverageScore, int RatingCount)> GetAverageAsync(int courseId, int? trainerId)
    {
        var where = "WHERE COURSE_ID = :CourseId";
        var parameters = new DynamicParameters();
        parameters.Add("CourseId", courseId);
        if (trainerId.HasValue)
        {
            where += " AND TRAINER_ID = :TrainerId";
            parameters.Add("TrainerId", trainerId.Value);
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var average = await connection.ExecuteScalarAsync<decimal?>(
            $"SELECT AVG(SCORE) FROM TRAINER_RATINGS {where}", parameters);
        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM TRAINER_RATINGS {where}", parameters);
        return (average, count);
    }
}
