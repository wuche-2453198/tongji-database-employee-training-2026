using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITestService
{
    Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId);

    Task<PagedResult<TrainingTest>> GetTestListAsync(
        int? employeeId, int? courseId, string? testType,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize);

    /// <summary>按员工+课程聚合的成绩汇总（PRE/POST/变化/提升率）。</summary>
    Task<PagedResult<TestScoreSummary>> GetScoreSummariesAsync(
        int? employeeId, int? courseId,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize);

    Task<TestImprovementResponse> GetImprovementAsync(int employeeId, int courseId);
}
