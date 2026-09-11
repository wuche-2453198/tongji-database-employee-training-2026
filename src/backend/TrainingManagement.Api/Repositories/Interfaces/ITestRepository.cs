using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ITestRepository
{
    Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId);

    Task<bool> UpdateScoreAsync(CreateTestRequest request, int recordedByEmpId);

    Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType);

    Task<PagedResult<TrainingTest>> GetPagedListAsync(
        int? employeeId, int? courseId, string? testType,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize);

    /// <summary>课程门禁:起止时间,用于 PRE/POST 录入时点校验。</summary>
    Task<ResultCourseGate> GetCourseGateAsync(int courseId);

    /// <summary>该员工该课程是否存在 HR_FILED 状态的培训申请(PRE 录入前置)。</summary>
    Task<bool> HasHrFiledRequestAsync(int employeeId, int courseId);

    /// <summary>读取 PRE/POST 成绩,未录入为 null。</summary>
    Task<(decimal? PreScore, decimal? PostScore)> GetScoresAsync(int employeeId, int courseId);
}
