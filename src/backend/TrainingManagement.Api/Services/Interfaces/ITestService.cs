using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITestService
{
    Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId);

    Task<PagedResult<TrainingTest>> GetTestListAsync(
        int? employeeId, int? courseId, string? testType, int page, int pageSize);

    Task<TestImprovementResponse> GetImprovementAsync(int employeeId, int courseId);
}
