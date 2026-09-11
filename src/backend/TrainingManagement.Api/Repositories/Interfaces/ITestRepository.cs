using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ITestRepository
{
    Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId, DateTime testedAt);

    Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType);

    Task<PagedResult<TrainingTest>> GetPagedListAsync(
        int? employeeId, int? courseId, string? testType,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize);

    /// <summary>按员工+课程聚合的 PRE/POST 成绩汇总，供成绩列表展示。</summary>
    Task<PagedResult<TestScoreSummary>> GetScoreSummariesAsync(
        int? employeeId, int? courseId,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize);

    /// <summary>课程门禁:起止时间,用于 PRE/POST 录入时点校验。</summary>
    Task<ResultCourseGate> GetCourseGateAsync(int courseId);

    /// <summary>该员工该课程的报名状态;无报名记录返回 null。</summary>
    Task<string?> GetRegistrationStatusAsync(int employeeId, int courseId);

    /// <summary>读取 PRE/POST 成绩,未录入为 null。</summary>
    Task<(decimal? PreScore, decimal? PostScore)> GetScoresAsync(int employeeId, int courseId);
}
