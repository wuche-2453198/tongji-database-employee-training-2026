using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Services.Interfaces;

// 部门培训预算服务接口
public interface IDepartmentTrainingService
{
    // 分页查询部门培训预算列表
    Task<PagedResult<DepartmentTrainingResponse>> GetPagedAsync(
        DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default);

    // 根据 ID 获取部门培训预算
    Task<DepartmentTrainingResponse?> GetByIdAsync(
        long deptId,
        CancellationToken cancellationToken = default);

    // 新增部门培训预算
    Task<DepartmentTrainingResponse> CreateAsync(
        CreateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default);

    // 更新部门培训预算
    Task<DepartmentTrainingResponse> UpdateAsync(
        long deptId,
        UpdateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default);

    // 删除部门培训预算
    Task<bool> DeleteAsync(
        long deptId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在调用方事务会话内占用部门预算，供课程发布使用（D-011：发布与预算占用必须同一事务）。
    /// </summary>
    Task<bool> TryOccupyBudgetAsync(
        long deptId,
        decimal amount,
        IDbSession session,
        CancellationToken cancellationToken = default);
}