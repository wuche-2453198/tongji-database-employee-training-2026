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

// 员工服务实现
public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<PagedResult<EmployeeResponse>> GetPagedAsync(
        EmployeeQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        // 部门主管仅能查看本部门员工，范围由服务端强制注入。
        if (!principal.IsInRole(RoleCodes.Hr) && !principal.IsInRole(RoleCodes.Admin))
        {
            if (!principal.IsInRole(RoleCodes.DepartmentManager))
            {
                throw new ForbiddenApiException("无权查看员工列表。");
            }

            var empId = principal.GetEmployeeId()
                ?? throw new UnauthorizedApiException("无法识别当前登录用户。");

            var operatorEmployee = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
            var department = operatorEmployee?.DeptName;
            if (string.IsNullOrWhiteSpace(department))
            {
                return new PagedResult<EmployeeResponse>(
                    Array.Empty<EmployeeResponse>(),
                    query.Page,
                    query.PageSize,
                    0);
            }

            query.DeptName = department;
        }

        var result = await _employeeRepository.GetPagedAsync(query, cancellationToken);

        return new PagedResult<EmployeeResponse>
        {
            Items = result.Items.Select(MapToResponse).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total
        };
    }

    public async Task<EmployeeResponse?> GetByIdAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
        return employee is null ? null : MapToResponse(employee);
    }

    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查登录名是否已被占用
        var loginNameExists = await _employeeRepository.IsLoginNameExistsAsync(
            request.LoginName,
            cancellationToken: cancellationToken);
        if (loginNameExists)
        {
            throw new BusinessException($"登录名 '{request.LoginName}' 已被占用");
        }

        // 创建员工实体
        var employee = new Employee
        {
            LoginName = request.LoginName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmpName = request.EmpName,
            DeptName = request.DeptName,
            Position = request.Position,
            Email = request.Email,
            Phone = request.Phone,
            HireDate = request.HireDate,
            Status = EmployeeStatusValues.Active,
            CreatedAt = DateTime.Now
        };

        // 保存到数据库
        var created = await _employeeRepository.CreateAsync(employee, cancellationToken);

        // 返回响应
        return MapToResponse(created);
    }

    public async Task<EmployeeResponse> UpdateAsync(
        long empId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        // 检查员工是否存在
        var existing = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundApiException($"员工 ID {empId} 不存在");
        }

        // 部分更新：只更新传入了值的字段
        if (!string.IsNullOrWhiteSpace(request.EmpName))
            existing.EmpName = request.EmpName;

        if (!string.IsNullOrWhiteSpace(request.DeptName))
            existing.DeptName = request.DeptName;

        if (!string.IsNullOrWhiteSpace(request.Position))
            existing.Position = request.Position;

        if (!string.IsNullOrWhiteSpace(request.Email))
            existing.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Phone))
            existing.Phone = request.Phone;

        if (request.HireDate.HasValue)
            existing.HireDate = request.HireDate.Value;

        if (!string.IsNullOrWhiteSpace(request.Status))
            existing.Status = request.Status;

        // 保存更新
        var updated = await _employeeRepository.UpdateAsync(existing, cancellationToken);
        if (!updated)
        {
            throw new BusinessException($"更新员工 ID {empId} 失败");
        }

        // 重新查询最新数据
        var result = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
        return MapToResponse(result!);
    }

    public async Task<bool> DeleteAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        // 检查员工是否存在
        var exists = await _employeeRepository.ExistsAsync(empId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundApiException($"员工 ID {empId} 不存在");
        }

        // 软删除（状态改为RESIGNED）
        return await _employeeRepository.DeleteAsync(empId, cancellationToken);
    }

    public async Task<bool> IsEmployeeActiveAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(empId, cancellationToken);
        return employee is not null && employee.Status == EmployeeStatusValues.Active;
    }

    // 将Employee实体映射为EmployeeResponse DTO
    private static EmployeeResponse MapToResponse(Employee employee)
    {
        return new EmployeeResponse
        {
            EmpId = employee.EmpId,
            EmpName = employee.EmpName,
            DeptName = employee.DeptName,
            Position = employee.Position,
            Email = employee.Email,
            Phone = employee.Phone,
            HireDate = employee.HireDate,
            Status = employee.Status,
            CreatedAt = employee.CreatedAt
        };
    }
}