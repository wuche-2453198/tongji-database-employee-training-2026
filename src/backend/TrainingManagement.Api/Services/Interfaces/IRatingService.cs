using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IRatingService
{
    Task<bool> CreateRatingAsync(CreateRatingRequest request, int employeeId);

    Task<PagedResult<TrainerRating>> GetRatingListAsync(int? courseId, int? trainerId, int page, int pageSize);

    Task<RatingAverageResponse> GetAverageAsync(int courseId, int? trainerId);

    Task<bool> VerifyRatingAsync(int ratingId, string verifyComment, int hrVerifierEmpId);
}
