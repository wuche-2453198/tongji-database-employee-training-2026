using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;

namespace TrainingManagement.Api.Repositories.Interfaces
{
    public interface ITestRepository
    {
        Task<bool> CreateAsync(CreateTestRequest request);
        Task<bool> UpdateScoreAsync(CreateTestRequest request);
        Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType);
        Task<object> GetListAsync(int? employeeId, int? courseId, string? testType);
    }
}
