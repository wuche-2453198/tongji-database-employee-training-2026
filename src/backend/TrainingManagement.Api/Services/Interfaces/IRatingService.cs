using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;

namespace TrainingManagement.Api.Services.Interfaces
{
    public interface IRatingService
    {
        Task<bool> CreateRatingAsync(CreateRatingRequest request, int employeeId);
        Task<object> GetRatingListAsync(int? courseId, int? trainerId, int page, int pageSize);
        Task<bool> VerifyRatingAsync(int ratingId, string verifyComment);
    }
}
