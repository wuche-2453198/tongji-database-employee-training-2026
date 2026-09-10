using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ITrainingRequestRepository
{
    Task<int> InsertAsync(TrainingRequest request, CancellationToken cancellationToken);

    Task<TrainingRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<TrainingRequest> Items, int Total)> SearchAsync(
        string? status,
        int? employeeId,
        int? courseId,
        int? deptId,
        string? employeeName,
        string? courseName,
        string? departmentName,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// 带状态前置条件的原子更新：影响行数为 0 表示状态已被并发修改。
    /// </summary>
    Task<bool> UpdateStatusAsync(
        int id,
        string expectedStatus,
        string newStatus,
        int? approverId,
        string? comment,
        CancellationToken cancellationToken);

    /// <summary>
    /// 活跃申请判定：PENDING / DEPT_APPROVED / HR_FILED 视为占用，仅 DEPT_REJECTED 允许重新提交。
    /// </summary>
    Task<bool> ExistsActiveRequestAsync(int employeeId, int courseId, CancellationToken cancellationToken);

    Task<EmployeeGate> GetEmployeeGateAsync(int employeeId, CancellationToken cancellationToken);

    Task<CourseGate> GetCourseGateAsync(int courseId, CancellationToken cancellationToken);

    Task<int?> GetEmployeeDeptIdAsync(int employeeId, CancellationToken cancellationToken);
}
