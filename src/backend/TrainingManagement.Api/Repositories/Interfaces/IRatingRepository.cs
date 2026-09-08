using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IRatingRepository
{
    Task<bool> CreateAsync(Dtos.Ratings.CreateRatingRequest request, int employeeId);

    Task<(IReadOnlyList<TrainerRating> Items, int Total)> GetListAsync(
        int? courseId, int? trainerId, int page, int pageSize);

    Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId);

    Task<TrainerRating?> GetByIdAsync(int ratingId);

    Task<bool> VerifyAsync(int ratingId, string verifyComment, int hrVerifierEmpId);

    /// <summary>员工在该课程是否存在 COMPLETED 报名(评分前置)。</summary>
    Task<bool> HasCompletedRegistrationAsync(int employeeId, int courseId);

    /// <summary>按课程(可选讲师)计算平均分与评分人数。</summary>
    Task<(decimal? AverageScore, int RatingCount)> GetAverageAsync(int courseId, int? trainerId);
}
