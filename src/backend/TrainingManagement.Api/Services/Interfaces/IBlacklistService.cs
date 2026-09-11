using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;

namespace TrainingManagement.Api.Services.Interfaces;

// 黑名单服务接口
public interface IBlacklistService
{
    // 分页查询黑名单列表
    Task<PagedResult<BlacklistResponse>> GetPagedAsync(
        BlacklistQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    // 根据ID获取黑名单记录
    Task<BlacklistResponse?> GetByIdAsync(
        long blackId,
        CancellationToken cancellationToken = default);

    // 新增黑名单记录
    Task<BlacklistResponse> CreateAsync(
        CreateBlacklistRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    // 更新黑名单记录
    Task<BlacklistResponse> UpdateAsync(
        long blackId,
        UpdateBlacklistRequest request,
        CancellationToken cancellationToken = default);

    // 删除黑名单记录
    Task<bool> DeleteAsync(
        long blackId,
        CancellationToken cancellationToken = default);

    // 检查员工是否在黑名单中
    Task<bool> IsEmployeeBlacklistedAsync(
        long empId,
        CancellationToken cancellationToken = default);
}