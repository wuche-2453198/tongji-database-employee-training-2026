using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;
public interface IRatingService
{
    Task<bool> CreateRatingAsync(CreateRatingRequest request, int employeeId);
    Task<IEnumerable<TrainerRating>> GetRatingListAsync(int? courseId, int? trainerId, int page, int pageSize);
    Task<bool> VerifyRatingAsync(int ratingId, string verifyComment, int hrVerifierEmpId);
}
