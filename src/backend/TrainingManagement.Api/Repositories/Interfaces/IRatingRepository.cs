using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;
public interface IRatingRepository
{
    Task<bool> CreateAsync(CreateRatingRequest request, int employeeId);
    Task<IEnumerable<TrainerRating>> GetListAsync(int? courseId, int? trainerId, int page, int pageSize);
    Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId);
    Task<TrainerRating?> GetByIdAsync(int ratingId);
    Task<bool> VerifyAsync(int ratingId, string verifyComment, int hrVerifierEmpId);
}
