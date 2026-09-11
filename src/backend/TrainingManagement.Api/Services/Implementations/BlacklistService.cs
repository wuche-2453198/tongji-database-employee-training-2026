using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

// 黑名单服务实现
public sealed class BlacklistService : IBlacklistService
{
    private const int MaxReasonLength = 500;

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
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        // 部门主管仅能查看本部门黑名单，范围由服务端裁决。
        if (!principal.IsInRole(RoleCodes.Hr) && !principal.IsInRole(RoleCodes.Admin))
        {
            if (!principal.IsInRole(RoleCodes.DepartmentManager))
            {
                throw new ForbiddenApiException("无权查看黑名单列表。");
            }

            var empId = principal.GetEmployeeId()
                ?? throw new UnauthorizedApiException("无法识别当前登录用户。");

            var operatorEmployee = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
            var department = operatorEmployee?.DeptName;
            if (string.IsNullOrWhiteSpace(department))
            {
                return new PagedResult<BlacklistResponse>(
                    Array.Empty<BlacklistResponse>(),
                    query.Page,
                    query.PageSize,
                    0);
            }

            query.DeptName = department;
        }

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
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var operatorEmpId = principal.GetEmployeeId()
            ?? throw new UnauthorizedApiException("无法识别当前登录用户。");

        // 先鉴权：仅管理员或部门主管可将员工加入黑名单。
        var isAdmin = principal.IsInRole(RoleCodes.Admin);
        var isManager = principal.IsInRole(RoleCodes.DepartmentManager);
        if (!isAdmin && !isManager)
        {
            throw new ForbiddenApiException("只有管理员或部门主管可以将员工加入黑名单。");
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException("黑名单原因不能为空。");
        }

        if (reason.Length > MaxReasonLength)
        {
            throw new BusinessException($"黑名单原因长度不能超过 {MaxReasonLength} 字符。");
        }

        if (operatorEmpId == request.EmpId)
        {
            throw new BusinessException("不能将自己加入黑名单。");
        }

        var target = await _employeeRepository.GetByIdAsync(request.EmpId, cancellationToken)
            ?? throw new NotFoundApiException($"员工 ID {request.EmpId} 不存在。");

        if (!isAdmin)
        {
            var operatorEmployee = await _employeeRepository.GetByIdAsync(operatorEmpId, cancellationToken);
            var operatorDept = operatorEmployee?.DeptName;
            if (string.IsNullOrWhiteSpace(operatorDept)
                || !string.Equals(operatorDept, target.DeptName, StringComparison.Ordinal))
            {
                throw new ForbiddenApiException("只能将本部门员工加入黑名单。");
            }
        }

        if (await _repository.IsEmployeeBlacklistedAsync(request.EmpId, cancellationToken))
        {
            throw new ConflictApiException($"员工 ID {request.EmpId} 当前已在黑名单中。");
        }

        var startDate = request.StartDate ?? DateTime.Today;
        var endDate = request.EndDate ?? startDate.AddDays(30);

        var entity = new Blacklist
        {
            EmpId = request.EmpId,
            Reason = reason,
            StartDate = startDate,
            EndDate = endDate,
            Status = "ACTIVE",
            OperatorEmpId = operatorEmpId,
            CreatedAt = DateTime.Now,
            EmpName = target.EmpName,
            DeptName = target.DeptName
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);

        var full = await _repository.GetByIdAsync(created.BlackId, cancellationToken);
        return MapToResponse(full ?? created);
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
            EmpName = entity.EmpName,
            DeptName = entity.DeptName,
            Reason = entity.Reason,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            OperatorEmpId = entity.OperatorEmpId,
            CreatedAt = entity.CreatedAt
        };
    }
}
