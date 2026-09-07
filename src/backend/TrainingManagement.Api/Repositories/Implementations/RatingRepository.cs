using Dapper;
using System.Data;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations
{
    public class RatingRepository : IRatingRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public RatingRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> CreateAsync(CreateRatingRequest request, int employeeId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO TRAINER_RATINGS 
                        (EMPLOYEE_ID, COURSE_ID, TRAINER_ID, SCORE, COMMENT, CREATED_AT, VERIFY_STATUS) 
                        VALUES (:EmployeeId, :CourseId, :TrainerId, :Score, :Comment, SYSDATE, 'PENDING')";

            var result = await connection.ExecuteAsync(sql, new
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
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT * FROM TRAINER_RATINGS WHERE 1=1";
            if (courseId.HasValue) sql += " AND COURSE_ID = :CourseId";
            if (trainerId.HasValue) sql += " AND TRAINER_ID = :TrainerId";
            sql += " ORDER BY RATING_ID DESC";

            var result = await connection.QueryAsync(sql, new { CourseId = courseId, TrainerId = trainerId });
            return result;
        }

        public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT COUNT(1) FROM TRAINER_RATINGS WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, CourseId = courseId });
            return count > 0;
        }

        public async Task<object> GetByIdAsync(int ratingId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM TRAINER_RATINGS WHERE RATING_ID = :RatingId";
            return await connection.QueryFirstOrDefaultAsync(sql, new { RatingId = ratingId });
        }

        public async Task<bool> VerifyAsync(int ratingId, string verifyComment)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE TRAINER_RATINGS 
                        SET VERIFY_STATUS = 'VERIFIED', VERIFY_COMMENT = :VerifyComment, VERIFIED_AT = SYSDATE 
                        WHERE RATING_ID = :RatingId";
            var result = await connection.ExecuteAsync(sql, new { RatingId = ratingId, VerifyComment = verifyComment });
            return result > 0;
        }
    }
}
