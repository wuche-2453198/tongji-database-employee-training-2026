using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;

namespace TrainingManagement.Api.Services.Interfaces
{
    public interface ITestService
    {
        Task<bool> CreateTestAsync(CreateTestRequest request);
        Task<object> GetTestListAsync(int? employeeId, int? courseId, string? testType);
    }
}
