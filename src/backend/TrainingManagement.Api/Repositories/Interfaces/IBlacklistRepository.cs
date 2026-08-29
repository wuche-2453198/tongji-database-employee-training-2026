using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

// 黑名单数据仓库接口
public interface IBlacklistRepository
{
    // 分页查询黑名单列表
    Task<PagedResult<Blacklist>> GetPagedAsync(
        BlacklistQuery query,
        CancellationToken cancellationToken = default);

    // 根据ID获取黑名单记录
    Task<Blacklist?> GetByIdAsync(
        long blackId,
        CancellationToken cancellationToken = default);

    // 根据员工ID获取生效中的黑名单记录
    Task<Blacklist?> GetActiveByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 检查员工是否在黑名单中
    Task<bool> IsEmployeeBlacklistedAsync(
        long empId,
        CancellationToken cancellationToken = default);

    // 检查黑名单记录是否存在
    Task<bool> ExistsAsync(
        long blackId,
        CancellationToken cancellationToken = default);

    // 新增黑名单记录
    Task<Blacklist> CreateAsync(
        Blacklist entity,
        CancellationToken cancellationToken = default);

    // 更新黑名单记录
    Task<bool> UpdateAsync(
        Blacklist entity,
        CancellationToken cancellationToken = default);

    // 删除黑名单记录
    Task<bool> DeleteAsync(
        long blackId,
        CancellationToken cancellationToken = default);
}