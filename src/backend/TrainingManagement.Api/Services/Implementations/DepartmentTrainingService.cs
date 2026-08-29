using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Organization;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

// 部门培训预算服务实现
public sealed class DepartmentTrainingService : IDepartmentTrainingService
{
    private readonly IDepartmentTrainingRepository _repository;

    public DepartmentTrainingService(IDepartmentTrainingRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<DepartmentTrainingResponse>> GetPagedAsync(
        DepartmentTrainingQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<DepartmentTrainingResponse>
        {
            Items = result.Items.Select(MapToResponse).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total
        };
    }

    public async Task<DepartmentTrainingResponse?> GetByIdAsync(
        long deptId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(deptId, cancellationToken);
        return entity is null ? null : MapToResponse(entity);
    }

    public async Task<DepartmentTrainingResponse> CreateAsync(
        CreateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        // 校验年度预算不能为负数
        if (request.AnnualBudget < 0)
        {
            throw new BusinessException("年度培训预算不能为负数");
        }

        // 检查部门名称是否已被占用
        var exists = await _repository.IsDeptNameExistsAsync(request.DeptName, cancellationToken: cancellationToken);
        if (exists)
        {
            throw new BusinessException($"部门 '{request.DeptName}' 已存在");
        }

        // 创建实体
        var entity = new DepartmentTraining
        {
            DeptName = request.DeptName,
            AnnualBudget = request.AnnualBudget,
            UsedBudget = 0,
            RemainBudget = request.AnnualBudget // 初始时剩余=年度预算
        };

        // 保存到数据库
        var created = await _repository.CreateAsync(entity, cancellationToken);

        // 返回响应
        return MapToResponse(created);
    }

    public async Task<DepartmentTrainingResponse> UpdateAsync(
        long deptId,
        UpdateDepartmentTrainingRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查是否存在
        var existing = await _repository.GetByIdAsync(deptId, cancellationToken);
        if (existing is null)
        {
            throw new BusinessException($"部门预算 ID {deptId} 不存在");
        }

        // 检查部门名称是否被其他部门占用
        if (!string.IsNullOrWhiteSpace(request.DeptName))
        {
            var exists = await _repository.IsDeptNameExistsAsync(
                request.DeptName,
                excludeDeptId: deptId,
                cancellationToken: cancellationToken);
            if (exists)
            {
                throw new BusinessException($"部门名称 '{request.DeptName}' 已被其他部门占用");
            }
            existing.DeptName = request.DeptName;
        }

        // 更新年度预算
        if (request.AnnualBudget.HasValue)
        {
            if (request.AnnualBudget.Value < 0)
            {
                throw new BusinessException("年度培训预算不能为负数");
            }
            existing.AnnualBudget = request.AnnualBudget.Value;
            // 剩余预算自动重新计算
            existing.RemainBudget = existing.AnnualBudget - existing.UsedBudget;
        }

        // 保存更新
        var updated = await _repository.UpdateAsync(existing, cancellationToken);
        if (!updated)
        {
            throw new BusinessException($"更新部门预算 ID {deptId} 失败");
        }

        // 重新查询最新数据
        var result = await _repository.GetByIdAsync(deptId, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<bool> DeleteAsync(
        long deptId,
        CancellationToken cancellationToken = default)
    {
        // 检查是否存在
        var exists = await _repository.ExistsAsync(deptId, cancellationToken);
        if (!exists)
        {
            throw new BusinessException($"部门预算 ID {deptId} 不存在");
        }

        // 执行删除
        return await _repository.DeleteAsync(deptId, cancellationToken);
    }

    // 将DepartmentTraining实体映射为DepartmentTrainingResponse DTO
    private static DepartmentTrainingResponse MapToResponse(DepartmentTraining entity)
    {
        return new DepartmentTrainingResponse
        {
            DeptId = entity.DeptId,
            DeptName = entity.DeptName,
            AnnualBudget = entity.AnnualBudget,
            UsedBudget = entity.UsedBudget,
            RemainBudget = entity.RemainBudget
        };
    }
}