using TrainingManagement.Api.Common.Enums;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class TrainingRequestService : ITrainingRequestService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly ITrainingRequestRepository _repository;

    // TODO(跨模块): 组织/课程/黑名单模块的公开 Service 合入后,
    // 将仓库层的 GetEmployeeGateAsync / GetCourseGateAsync 门禁查询替换为对应 Service 契约调用。
    public TrainingRequestService(ITrainingRequestRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrainingRequestResponseDto> SubmitRequestAsync(
        int employeeId, CreateTrainingRequestDto dto, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetEmployeeGateAsync(employeeId, cancellationToken);
        if (!employee.Exists)
        {
            throw new NotFoundApiException("员工不存在。");
        }

        if (!string.Equals(employee.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("员工当前不在职,无法提交培训申请。");
        }

        var course = await _repository.GetCourseGateAsync(dto.CourseId, cancellationToken);
        if (!course.Exists)
        {
            throw new NotFoundApiException("课程不存在。");
        }

        if (!string.Equals(course.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("课程尚未发布,无法申请。");
        }

        if (course.StartAt.HasValue && course.StartAt.Value <= DateTime.Now)
        {
            throw new BusinessException("课程已开始或结束,无法申请。");
        }

        if (await _repository.ExistsActiveRequestAsync(employeeId, dto.CourseId, cancellationToken))
        {
            throw new ConflictApiException("您已提交过该课程的申请,请勿重复提交。");
        }

        var request = new TrainingRequest
        {
            EmployeeId = employeeId,
            CourseId = dto.CourseId,
            RequestReason = dto.RequestReason,
            Status = TrainingRequestStatusText.Pending,
        };

        var newId = await _repository.InsertAsync(request, cancellationToken);
        var created = await _repository.GetByIdAsync(newId, cancellationToken)
            ?? throw new BusinessException("申请创建失败,请重试。");

        return MapToDto(created);
    }

    public Task<PagedResult<TrainingRequestResponseDto>> GetMyRequestsAsync(
        int employeeId, TrainingRequestQueryDto query, CancellationToken cancellationToken)
    {
        return SearchAsync(
            query.Status,
            employeeId,
            query.CourseId,
            deptId: null,
            query,
            cancellationToken);
    }

    public async Task<PagedResult<TrainingRequestResponseDto>> GetAllRequestsAsync(
        ActorContext actor, TrainingRequestQueryDto query, CancellationToken cancellationToken)
    {
        // 数据范围:主管只看本部门;HR/管理员可跨部门。
        int? deptId = null;
        if (actor.IsManager && !actor.IsAdmin && !actor.IsHr)
        {
            var managerDeptId = await _repository.GetEmployeeDeptIdAsync(actor.EmployeeId, cancellationToken);
            if (!managerDeptId.HasValue)
            {
                throw new ForbiddenApiException("无法确定您所属的部门,无法查询申请。");
            }

            deptId = managerDeptId;
        }

        return await SearchAsync(
            query.Status,
            query.EmployeeId,
            query.CourseId,
            deptId,
            query,
            cancellationToken);
    }

    public async Task<TrainingRequestResponseDto> GetRequestByIdAsync(
        ActorContext actor, int id, CancellationToken cancellationToken)
    {
        var request = await GetRequestOrThrowAsync(id, cancellationToken);

        await EnsureCanViewAsync(actor, request, cancellationToken);

        return MapToDto(request);
    }

    public Task<TrainingRequestResponseDto> DeptApproveAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken)
    {
        return ApproveAsync(actor, requestId, comment, TrainingRequestStatusText.DeptApproved, cancellationToken);
    }

    public Task<TrainingRequestResponseDto> DeptRejectAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken)
    {
        return ApproveAsync(actor, requestId, comment, TrainingRequestStatusText.DeptRejected, cancellationToken);
    }

    public async Task<TrainingRequestResponseDto> HrFileAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken)
    {
        var request = await GetRequestOrThrowAsync(requestId, cancellationToken);

        if (!string.Equals(request.Status, TrainingRequestStatusText.DeptApproved, StringComparison.Ordinal))
        {
            throw new BusinessException($"当前状态为 {request.Status},无法进行备案操作。");
        }

        var updated = await _repository.UpdateStatusAsync(
            requestId,
            TrainingRequestStatusText.DeptApproved,
            TrainingRequestStatusText.HrFiled,
            actor.EmployeeId,
            comment,
            cancellationToken);

        if (!updated)
        {
            throw new ConflictApiException("申请状态已被其他人变更,请刷新后重试。");
        }

        var filed = await GetRequestOrThrowAsync(requestId, cancellationToken);
        return MapToDto(filed);
    }

    private async Task<TrainingRequestResponseDto> ApproveAsync(
        ActorContext actor, int requestId, string? comment, string targetStatus, CancellationToken cancellationToken)
    {
        var request = await GetRequestOrThrowAsync(requestId, cancellationToken);

        if (!string.Equals(request.Status, TrainingRequestStatusText.Pending, StringComparison.Ordinal))
        {
            var action = targetStatus == TrainingRequestStatusText.DeptApproved ? "审批" : "驳回";
            throw new BusinessException($"当前状态为 {request.Status},无法进行{action}操作。");
        }

        // 数据范围:主管只能审批本部门员工的申请;管理员可代办(跳过部门校验)。
        if (!actor.IsAdmin)
        {
            var managerDeptId = await _repository.GetEmployeeDeptIdAsync(actor.EmployeeId, cancellationToken);
            if (managerDeptId != request.DeptId)
            {
                throw new ForbiddenApiException("您只能审批本部门员工的申请。");
            }
        }

        var updated = await _repository.UpdateStatusAsync(
            requestId,
            TrainingRequestStatusText.Pending,
            targetStatus,
            actor.EmployeeId,
            comment,
            cancellationToken);

        if (!updated)
        {
            throw new ConflictApiException("申请状态已被其他人变更,请刷新后重试。");
        }

        var after = await GetRequestOrThrowAsync(requestId, cancellationToken);
        return MapToDto(after);
    }

    private async Task<TrainingRequest> GetRequestOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundApiException("申请不存在。");
    }

    private async Task EnsureCanViewAsync(ActorContext actor, TrainingRequest request, CancellationToken cancellationToken)
    {
        if (actor.IsAdmin || actor.IsHr)
        {
            return;
        }

        // 数据范围:主管只看本部门;普通员工只看本人。
        if (actor.IsManager)
        {
            var managerDeptId = await _repository.GetEmployeeDeptIdAsync(actor.EmployeeId, cancellationToken);
            if (managerDeptId != request.DeptId)
            {
                throw new ForbiddenApiException("您只能查看本部门员工的申请。");
            }

            return;
        }

        if (request.EmployeeId != actor.EmployeeId)
        {
            throw new ForbiddenApiException("您只能查看本人的申请。");
        }
    }

    private async Task<PagedResult<TrainingRequestResponseDto>> SearchAsync(
        string? status,
        int? employeeId,
        int? courseId,
        int? deptId,
        TrainingRequestQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(status) && !TrainingRequestStatusText.IsValid(status))
        {
            throw new BusinessException("无效的申请状态筛选值。");
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);

        var (items, total) = await _repository.SearchAsync(
            status,
            employeeId,
            courseId,
            deptId,
            query.EmployeeName,
            query.CourseName,
            query.DepartmentName,
            query.StartDateFrom,
            query.StartDateTo,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<TrainingRequestResponseDto>
        {
            Items = items.Select(MapToDto).ToArray(),
            Page = page,
            PageSize = pageSize,
            Total = total,
        };
    }

    private static TrainingRequestResponseDto MapToDto(TrainingRequest entity)
    {
        return new TrainingRequestResponseDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeName = entity.EmployeeName,
            DeptId = entity.DeptId,
            DepartmentName = entity.DepartmentName,
            CourseId = entity.CourseId,
            CourseName = entity.CourseName,
            RequestReason = entity.RequestReason,
            Status = entity.Status,
            DeptApproverId = entity.DeptApproverId,
            DeptApproverName = entity.DeptApproverName,
            DeptApproveTime = entity.DeptApproveTime,
            DeptApproveComment = entity.DeptApproveComment,
            HrApproverId = entity.HrApproverId,
            HrApproverName = entity.HrApproverName,
            HrFileTime = entity.HrFileTime,
            HrFileComment = entity.HrFileComment,
            CreateTime = entity.CreateTime,
        };
    }
}
