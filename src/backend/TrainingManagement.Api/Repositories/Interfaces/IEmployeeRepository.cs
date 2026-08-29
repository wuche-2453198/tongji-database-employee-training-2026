using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

// 员工数据仓库接口
public interface IEmployeeRepository
{
    // 分页查询员工列表
    Task<PagedResult<Employee>> GetPagedAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken = default);

    // 根据ID获取员工
    Task<Employee?> GetByIdAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 检查员工是否存在
    Task<bool> ExistsAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 检查登录名是否已被占用
    Task<bool> IsLoginNameExistsAsync(
        string loginName,
        long? excludeEmpId = null,
        CancellationToken cancellationToken = default);

    // 新增员工
    Task<Employee> CreateAsync(
        Employee employee,
        CancellationToken cancellationToken = default);

    // 更新员工信息
    Task<bool> UpdateAsync(
        Employee employee,
        CancellationToken cancellationToken = default);

    // 删除员工（软删除，更新状态为RESIGNED）
    Task<bool> DeleteAsync(
        long empId,
        CancellationToken cancellationToken = default);
}