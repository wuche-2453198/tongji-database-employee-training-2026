using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;

namespace TrainingManagement.Api.Repositories.Interfaces
{
    public interface IRatingRepository
    {
        Task<bool> CreateAsync(CreateRatingRequest request, int employeeId);
        Task<object> GetListAsync(int? courseId, int? trainerId, int page, int pageSize);
        Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId);
        Task<object> GetByIdAsync(int ratingId);
        Task<bool> VerifyAsync(int ratingId, string verifyComment);
    }
}
