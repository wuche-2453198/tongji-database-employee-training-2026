using System.Security.Claims;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Attendance;
using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class RegistrationService : IRegistrationService
{
    private const string RegisteredStatus = "REGISTERED";
    private const string SignedInStatus = "SIGNED_IN";
    private const string ActiveEmployeeStatus = "ACTIVE";
    private const string PublishedCourseStatus = "PUBLISHED";

    private readonly IRegistrationRepository _registrationRepository;

    public RegistrationService(IRegistrationRepository registrationRepository)
    {
        _registrationRepository = registrationRepository;
    }

    public async Task<PagedResult<RegistrationResponse>> GetMyAsync(
        RegistrationQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var empId = principal.GetEmployeeId()
            ?? throw new UnauthorizedApiException(
                "无法识别当前登录用户。");

        ValidateQuery(query);

        var result = await _registrationRepository.GetAllAsync(
            query,
            empId,
            cancellationToken);

        return ToPagedResult(
            result,
            query);
    }

    public async Task<PagedResult<RegistrationResponse>> GetAllAsync(
        RegistrationQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        ValidateQuery(query);

        var result = await _registrationRepository.GetAllAsync(
            query,
            null,
            cancellationToken);

        return ToPagedResult(
            result,
            query);
    }

    public async Task<RegistrationResponse?> GetByIdAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var record = await _registrationRepository.GetByIdAsync(
            regId,
            cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (!IsHrOrAdmin(principal))
        {
            var empId = principal.GetEmployeeId();

            if (empId is null || empId.Value != record.EmpId)
            {
                return null;
            }
        }

        return ToResponse(record);
    }

    public async Task<RegistrationResponse> CreateAsync(
        CreateRegistrationRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var empId = principal.GetEmployeeId()
            ?? throw new UnauthorizedApiException(
                "无法识别当前登录用户。");

        var requestId = await _registrationRepository.FindFiledRequestIdAsync(
            empId,
            request.CourseId,
            cancellationToken);

        if (requestId is null)
        {
            throw new ConflictApiException(
                "该课程尚未完成 HR 备案，不能报名。");
        }

        var employee = await _registrationRepository.GetEmployeeEligibilityAsync(
            empId,
            cancellationToken);

        if (employee is null
            || !string.Equals(
                employee.Status,
                ActiveEmployeeStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictApiException(
                "员工信息不存在或已离职，不能报名。");
        }

        if (employee.ActiveBlacklistCount > 0)
        {
            throw new ConflictApiException(
                "员工处于黑名单生效期，不能报名。");
        }

        var course = await _registrationRepository.GetCourseEligibilityAsync(
            request.CourseId,
            cancellationToken);

        if (course is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        if (!string.Equals(
                course.CourseStatus,
                PublishedCourseStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictApiException(
                "课程未发布或已关闭，不能报名。");
        }

        if (course.StartAt is null
            || course.StartAt <= DateTime.Now)
        {
            throw new ConflictApiException(
                "课程已开始，不能报名。");
        }

        var registration = new RegistrationRecord
        {
            RequestId = requestId.Value,
            EmpId = empId,
            CourseId = request.CourseId,
            Status = RegisteredStatus
        };

        var result = await _registrationRepository.CreateAsync(
            registration,
            cancellationToken);

        switch (result.Outcome)
        {
            case RegistrationCreateOutcome.CourseUnavailable:
                throw new ConflictApiException(
                    "课程当前不可报名，请刷新后重试。");
            case RegistrationCreateOutcome.CapacityFull:
                throw new ConflictApiException(
                    "课程已满员，不能报名。");
            case RegistrationCreateOutcome.Duplicate:
                throw new ConflictApiException(
                    "已报名该课程，不能重复报名。");
        }

        var created = await GetByIdAsync(
            result.RegId,
            principal,
            cancellationToken);

        if (created is null)
        {
            throw new BusinessException(
                "报名创建失败。");
        }

        return created;
    }

    public async Task<RegistrationResponse> CancelAsync(
        long regId,
        CancelRegistrationRequest? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var context = await GetContextOrThrowAsync(
            regId,
            cancellationToken);

        EnsureCanManage(
            context,
            principal);

        if (!IsStatus(
                context.Status,
                RegisteredStatus))
        {
            throw new ConflictApiException(
                "只有已报名状态可以取消。");
        }

        if (context.StartAt is null
            || context.StartAt <= DateTime.Now)
        {
            throw new ConflictApiException(
                "课程已开始，不能取消。");
        }

        var cancelled = await _registrationRepository.CancelAsync(
            regId,
            TrimToNull(request?.Reason),
            cancellationToken);

        if (!cancelled)
        {
            throw new ConflictApiException(
                "报名状态已变化，请刷新后重试。");
        }

        return await GetByIdOrThrowAsync(
            regId,
            principal,
            cancellationToken);
    }

    public async Task<RegistrationResponse> MarkAbsentAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        var context = await GetContextOrThrowAsync(
            regId,
            cancellationToken);

        if (!IsStatus(
                context.Status,
                RegisteredStatus))
        {
            throw new ConflictApiException(
                "只有已报名状态可以标记缺勤。");
        }

        if (context.EndAt is null
            || context.EndAt > DateTime.Now)
        {
            throw new ConflictApiException(
                "课程尚未结束，不能标记缺勤。");
        }

        var marked = await _registrationRepository.MarkAbsentAsync(
            regId,
            cancellationToken);

        if (!marked)
        {
            throw new ConflictApiException(
                "报名状态已变化，请刷新后重试。");
        }

        return await GetByIdOrThrowAsync(
            regId,
            principal,
            cancellationToken);
    }

    public async Task<RegistrationResponse> CompleteAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        var context = await GetContextOrThrowAsync(
            regId,
            cancellationToken);

        if (!IsStatus(
                context.Status,
                SignedInStatus))
        {
            throw new ConflictApiException(
                "只有已签到状态可以完成培训。");
        }

        if (context.EndAt is null
            || context.EndAt > DateTime.Now)
        {
            throw new ConflictApiException(
                "课程尚未结束，不能完成培训。");
        }

        var completed = await _registrationRepository.CompleteAsync(
            regId,
            cancellationToken);

        if (!completed)
        {
            throw new ConflictApiException(
                "报名状态已变化，请刷新后重试。");
        }

        return await GetByIdOrThrowAsync(
            regId,
            principal,
            cancellationToken);
    }

    public async Task<RegistrationSummaryResponse> GetSummaryAsync(
        long? courseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        var summary = await _registrationRepository.GetSummaryAsync(
            courseId,
            cancellationToken);

        if (courseId.HasValue
            && summary.MaxStudents is null)
        {
            throw new NotFoundApiException(
                "课程不存在。");
        }

        long? remainingSeats = summary.MaxStudents.HasValue
            ? Math.Max(
                0,
                summary.MaxStudents.Value
                    - (summary.Total - summary.Canceled))
            : null;

        return new RegistrationSummaryResponse
        {
            Total = summary.Total,
            Registered = summary.Registered,
            SignedIn = summary.SignedIn,
            Absent = summary.Absent,
            Completed = summary.Completed,
            Canceled = summary.Canceled,
            RemainingSeats = remainingSeats
        };
    }

    private async Task<RegistrationContextRecord> GetContextOrThrowAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        return await _registrationRepository.GetContextAsync(
                regId,
                cancellationToken)
            ?? throw new NotFoundApiException(
                "报名记录不存在。");
    }

    private async Task<RegistrationResponse> GetByIdOrThrowAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        return await GetByIdAsync(
                regId,
                principal,
                cancellationToken)
            ?? throw new NotFoundApiException(
                "报名记录不存在。");
    }

    private static void ValidateQuery(RegistrationQuery query)
    {
        if (query.StartAtFrom.HasValue
            && query.StartAtTo.HasValue
            && query.StartAtFrom > query.StartAtTo)
        {
            throw new BusinessException(
                "查询开始时间不能晚于查询结束时间。");
        }
    }

    private static PagedResult<RegistrationResponse> ToPagedResult(
        (IReadOnlyList<RegistrationRecord> Items, long Total) result,
        RegistrationQuery query)
    {
        var items = result.Items
            .Select(ToResponse)
            .ToArray();

        return new PagedResult<RegistrationResponse>(
            items,
            query.Page,
            query.PageSize,
            result.Total);
    }

    private static RegistrationResponse ToResponse(
        RegistrationRecord record)
    {
        return new RegistrationResponse
        {
            RegId = record.RegId,
            RequestId = record.RequestId,
            EmpId = record.EmpId,
            EmpName = record.EmpName,
            DeptId = record.DeptId,
            DeptName = record.DeptName,
            CourseId = record.CourseId,
            CourseName = record.CourseName,
            CourseType = record.CourseType,
            DurationHours = record.DurationHours,
            TrainerName = record.TrainerName,
            StartAt = record.StartAt,
            EndAt = record.EndAt,
            Location = record.Location,
            CourseStatus = record.CourseStatus,
            MaxStudents = record.MaxStudents,
            Status = record.Status,
            RegisteredAt = record.RegisteredAt,
            CompletedAt = record.CompletedAt,
            ActualHours = record.ActualHours,
            CanceledAt = record.CanceledAt,
            CancelReason = record.CancelReason,
            Attendance = record.AttendId.HasValue
                ? new AttendanceResponse
                {
                    AttendId = record.AttendId.Value,
                    RegId = record.RegId,
                    SigninType = record.SigninType ?? string.Empty,
                    SignedInAt = record.SignedInAt
                        ?? record.RegisteredAt,
                    LatenessMinutes = record.LatenessMinutes ?? 0,
                    DeductHours = record.DeductHours ?? 0,
                    Remark = record.AttendanceRemark,
                    CreatedAt = record.SignedInAt
                        ?? record.RegisteredAt
                }
                : null,
            Actions = BuildActions(record)
        };
    }

    private static RegistrationActions BuildActions(
        RegistrationRecord record)
    {
        var now = DateTime.Now;
        var registered = IsStatus(
            record.Status,
            RegisteredStatus);
        var signedIn = IsStatus(
            record.Status,
            SignedInStatus);

        var canCancel = registered
            && record.StartAt.HasValue
            && now < record.StartAt.Value;
        string? cancelReason = null;

        if (!registered)
        {
            cancelReason = "当前状态不允许取消。";
        }
        else if (record.StartAt is null
            || now >= record.StartAt.Value)
        {
            cancelReason = "课程已开始，不能取消。";
        }

        var canSignIn = registered
            && record.EndAt.HasValue
            && now <= record.EndAt.Value;
        string? signInReason = null;

        if (!registered)
        {
            signInReason = "当前状态不允许签到。";
        }
        else if (record.EndAt is null
            || now > record.EndAt.Value)
        {
            signInReason = "课程已结束，不能签到。";
        }

        var canComplete = signedIn
            && record.EndAt.HasValue
            && now >= record.EndAt.Value;
        string? completeReason = null;

        if (!signedIn)
        {
            completeReason = "只有已签到状态可以完成培训。";
        }
        else if (record.EndAt is null
            || now < record.EndAt.Value)
        {
            completeReason = "课程尚未结束，不能完成培训。";
        }

        var canMarkAbsent = registered
            && record.EndAt.HasValue
            && now >= record.EndAt.Value;
        string? absentReason = null;

        if (!registered)
        {
            absentReason = "当前状态不允许标记缺勤。";
        }
        else if (record.EndAt is null
            || now < record.EndAt.Value)
        {
            absentReason = "课程尚未结束，不能标记缺勤。";
        }

        return new RegistrationActions
        {
            Cancel = new ActionEligibility
            {
                Allowed = canCancel,
                Reason = cancelReason
            },
            SignIn = new ActionEligibility
            {
                Allowed = canSignIn,
                Reason = signInReason
            },
            Complete = new ActionEligibility
            {
                Allowed = canComplete,
                Reason = completeReason
            },
            MarkAbsent = new ActionEligibility
            {
                Allowed = canMarkAbsent,
                Reason = absentReason
            }
        };
    }

    private static bool IsStatus(
        string actual,
        string expected)
    {
        return string.Equals(
            actual,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHrOrAdmin(
        ClaimsPrincipal principal)
    {
        return principal.IsInRole(RoleCodes.Hr)
            || principal.IsInRole(RoleCodes.Admin);
    }

    private static void EnsureHrOrAdmin(
        ClaimsPrincipal principal)
    {
        if (!IsHrOrAdmin(principal))
        {
            throw new ForbiddenApiException(
                "只有 HR 或管理员可以执行该操作。");
        }
    }

    private static void EnsureCanManage(
        RegistrationContextRecord context,
        ClaimsPrincipal principal)
    {
        if (IsHrOrAdmin(principal))
        {
            return;
        }

        var empId = principal.GetEmployeeId();

        if (empId is null
            || empId.Value != context.EmpId)
        {
            throw new ForbiddenApiException(
                "只能操作本人的报名记录。");
        }
    }

    private static string? TrimToNull(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
