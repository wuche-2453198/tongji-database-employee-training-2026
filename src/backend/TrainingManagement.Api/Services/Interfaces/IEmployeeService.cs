using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;

namespace TrainingManagement.Api.Services.Interfaces;

// 员工服务接口
public interface IEmployeeService
{
    // 分页查询员工列表
    Task<PagedResult<EmployeeResponse>> GetPagedAsync(
        EmployeeQuery query,
        CancellationToken cancellationToken = default);

    // 根据ID获取员工
    Task<EmployeeResponse?> GetByIdAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 新增员工
    Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    // 更新员工信息
    Task<EmployeeResponse> UpdateAsync(
        long empId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    // 删除员工（软删除，状态置为离职）
    Task<bool> DeleteAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 检查员工是否在职
    Task<bool> IsEmployeeActiveAsync(
        long empId,
        CancellationToken cancellationToken = default);
}