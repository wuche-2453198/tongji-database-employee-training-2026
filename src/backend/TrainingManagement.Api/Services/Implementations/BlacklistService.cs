using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

// 黑名单服务实现
public sealed class BlacklistService : IBlacklistService
{
    private readonly IBlacklistRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public BlacklistService(
        IBlacklistRepository repository,
        IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<PagedResult<BlacklistResponse>> GetPagedAsync(
        BlacklistQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<BlacklistResponse>
        {
            Items = result.Items.Select(MapToResponse).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total
        };
    }

    public async Task<BlacklistResponse?> GetByIdAsync(
        long blackId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(blackId, cancellationToken);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<BlacklistResponse> CreateAsync(
        CreateBlacklistRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查员工是否存在
        var employeeExists = await _employeeRepository.ExistsAsync(request.EmpId, cancellationToken);
        if (!employeeExists)
        {
            throw new BusinessException($"员工 ID {request.EmpId} 不存在");
        }

        // 检查员工是否已在黑名单中
        var isBlacklisted = await _repository.IsEmployeeBlacklistedAsync(request.EmpId, cancellationToken);
        if (isBlacklisted)
        {
            throw new BusinessException($"员工 ID {request.EmpId} 当前已在黑名单中");
        }

        // 设置日期默认值
        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? startDate.AddDays(30);

        // 创建实体
        var entity = new Blacklist
        {
            EmpId = request.EmpId,
            Reason = request.Reason,
            StartDate = startDate,
            EndDate = endDate,
            Status = "ACTIVE"
        };

        // 保存到数据库
        var created = await _repository.CreateAsync(entity, cancellationToken);

        // 返回响应
        return MapToResponse(created);
    }

    public async Task<BlacklistResponse> UpdateAsync(
        long blackId,
        UpdateBlacklistRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查是否存在
        var existing = await _repository.GetByIdAsync(blackId, cancellationToken);
        if (existing is null)
        {
            throw new BusinessException($"黑名单记录 ID {blackId} 不存在");
        }

        // 部分更新
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            existing.Reason = request.Reason;
        }

        if (request.EndDate.HasValue)
        {
            existing.EndDate = request.EndDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            // 如果状态变为RELEASED，同时清除结束日期（表示已解除）
            if (request.Status == "RELEASED" && existing.Status == "ACTIVE")
            {
                existing.EndDate = DateTime.Today;
            }
            existing.Status = request.Status;
        }

        // 保存更新
        var updated = await _repository.UpdateAsync(existing, cancellationToken);
        if (!updated)
        {
            throw new BusinessException($"更新黑名单记录 ID {blackId} 失败");
        }

        // 重新查询最新数据
        var result = await _repository.GetByIdAsync(blackId, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<bool> DeleteAsync(
        long blackId,
        CancellationToken cancellationToken = default)
    {
        // 检查是否存在
        var exists = await _repository.ExistsAsync(blackId, cancellationToken);
        if (!exists)
        {
            throw new BusinessException($"黑名单记录 ID {blackId} 不存在");
        }

        // 执行删除
        return await _repository.DeleteAsync(blackId, cancellationToken);
    }

    public async Task<bool> IsEmployeeBlacklistedAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.IsEmployeeBlacklistedAsync(empId, cancellationToken);
    }

    // 将Blacklist实体映射为BlacklistResponse DTO
    private static BlacklistResponse MapToResponse(Blacklist entity)
    {
        return new BlacklistResponse
        {
            BlackId = entity.BlackId,
            EmpId = entity.EmpId,
            Reason = entity.Reason,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status
        };
    }
}