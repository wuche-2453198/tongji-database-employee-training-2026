using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

// 部门培训预算数据仓库接口
public interface IDepartmentTrainingRepository
{
    // 分页查询部门培训预算列表
    Task<PagedResult<DepartmentTraining>> GetPagedAsync(
        DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default);

    // 根据ID获取部门培训预算
    Task<DepartmentTraining?> GetByIdAsync(
        long deptId,
        CancellationToken cancellationToken = default);

    // 根据部门名称获取
    Task<DepartmentTraining?> GetByNameAsync(
        string deptName,
        CancellationToken cancellationToken = default);

    // 检查部门是否存在
    Task<bool> ExistsAsync(
        long deptId,
        CancellationToken cancellationToken = default);

    // 检查部门名称是否已被占用
    Task<bool> IsDeptNameExistsAsync(
        string deptName,
        long? excludeDeptId = null,
        CancellationToken cancellationToken = default);

    // 新增部门培训预算
    Task<DepartmentTraining> CreateAsync(
        DepartmentTraining entity,
        CancellationToken cancellationToken = default);

    // 更新部门培训预算
    Task<bool> UpdateAsync(
        DepartmentTraining entity,
        CancellationToken cancellationToken = default);

    // 删除部门培训预算
    Task<bool> DeleteAsync(
        long deptId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在调用方事务内占用部门预算。仅当“已用预算 + 本次金额”不超过年度预算时更新成功，
    /// 返回 false 表示预算不足或部门不存在（由调用方回滚整个事务）。
    /// </summary>
    Task<bool> TryOccupyBudgetAsync(
        long deptId,
        decimal amount,
        IDbSession session,
        CancellationToken cancellationToken = default);
}