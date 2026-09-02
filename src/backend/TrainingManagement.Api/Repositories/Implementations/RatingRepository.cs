using Dapper;
using System.Data;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations
{
    public class RatingRepository : IRatingRepository
    {
        private readonly IDbConnection _connection;

        public RatingRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<bool> CreateAsync(CreateRatingRequest request, int employeeId)
        {
            var sql = @"INSERT INTO TRAINER_RATINGS 
                        (EMPLOYEE_ID, COURSE_ID, TRAINER_ID, SCORE, COMMENT, CREATED_AT, VERIFY_STATUS) 
                        VALUES (:EmployeeId, :CourseId, :TrainerId, :Score, :Comment, SYSDATE, 'PENDING')";

            var result = await _connection.ExecuteAsync(sql, new
            {
                EmployeeId = employeeId,
                request.CourseId,
                request.TrainerId,
                request.Score,
                request.Comment
            });

            return result > 0;
        }

        public async Task<object> GetListAsync(int? courseId, int? trainerId, int page, int pageSize)
        {
            var sql = @"SELECT * FROM TRAINER_RATINGS WHERE 1=1";
            if (courseId.HasValue) sql += " AND COURSE_ID = :CourseId";
            if (trainerId.HasValue) sql += " AND TRAINER_ID = :TrainerId";
            sql += " ORDER BY RATING_ID DESC";

            var result = await _connection.QueryAsync(sql, new { CourseId = courseId, TrainerId = trainerId });
            return result;
        }

        public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            var sql = "SELECT COUNT(1) FROM TRAINER_RATINGS WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, CourseId = courseId });
            return count > 0;
        }

        public async Task<object> GetByIdAsync(int ratingId)
        {
            var sql = "SELECT * FROM TRAINER_RATINGS WHERE RATING_ID = :RatingId";
            return await _connection.QueryFirstOrDefaultAsync(sql, new { RatingId = ratingId });
        }

        public async Task<bool> VerifyAsync(int ratingId, string verifyComment)
        {
            var sql = @"UPDATE TRAINER_RATINGS 
                        SET VERIFY_STATUS = 'VERIFIED', VERIFY_COMMENT = :VerifyComment, VERIFIED_AT = SYSDATE 
                        WHERE RATING_ID = :RatingId";
            var result = await _connection.ExecuteAsync(sql, new { RatingId = ratingId, VerifyComment = verifyComment });
            return result > 0;
        }
    }
}
