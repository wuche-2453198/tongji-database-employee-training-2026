using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Attendance;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class AttendanceService : IAttendanceService
{
    private const string RegisteredStatus = "REGISTERED";
    private const string ScanSigninType = "SCAN";
    private const string ManualSigninType = "MANUAL";

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IRegistrationRepository _registrationRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IRegistrationRepository registrationRepository)
    {
        _attendanceRepository = attendanceRepository;
        _registrationRepository = registrationRepository;
    }

    public async Task<AttendanceResponse> SignInAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        var context = await GetContextOrThrowAsync(
            regId,
            cancellationToken);

        return await PerformSignInAsync(
            context,
            ScanSigninType,
            DateTime.Now,
            null,
            cancellationToken);
    }

    public async Task<AttendanceResponse> ManualSignInAsync(
        CreateManualAttendanceRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        EnsureHrOrAdmin(principal);

        if (string.IsNullOrWhiteSpace(request.Remark))
        {
            throw new BusinessException(
                "补签必须填写备注。");
        }

        var signInAt = request.SigninTime
            ?? DateTime.Now;

        if (signInAt > DateTime.Now)
        {
            throw new BusinessException(
                "补签时间不能晚于当前时间。");
        }

        var context = await GetContextOrThrowAsync(
            request.RegId,
            cancellationToken);

        if (context.StartAt is null)
        {
            throw new BusinessException(
                "课程尚未设置开始时间，不能补签。");
        }

        if (signInAt < context.StartAt.Value.AddMinutes(-30))
        {
            throw new BusinessException(
                "补签时间不能早于课程开始前30分钟。");
        }

        return await PerformSignInAsync(
            context,
            ManualSigninType,
            signInAt,
            request.Remark.Trim(),
            cancellationToken);
    }

    private async Task<AttendanceResponse> PerformSignInAsync(
        RegistrationContextRecord context,
        string signinType,
        DateTime signInAt,
        string? remark,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(
                context.Status,
                RegisteredStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictApiException(
                "只有已报名状态可以签到。");
        }

        if (context.EndAt is null
            || signInAt > context.EndAt.Value)
        {
            throw new ConflictApiException(
                "课程已结束，不能签到。");
        }

        var startAt = context.StartAt
            ?? signInAt;

        var (latenessMinutes, deductHours, actualHours) =
            TrainingHoursCalculator.Calculate(
                signInAt,
                startAt,
                context.DurationHours);

        var attendance = new AttendanceRecord
        {
            RegId = context.RegId,
            SigninType = signinType,
            SignedInAt = signInAt,
            LatenessMinutes = latenessMinutes,
            DeductHours = deductHours,
            Remark = remark
        };

        var result = await _attendanceRepository.SignInAsync(
            attendance,
            actualHours,
            cancellationToken);

        switch (result.Outcome)
        {
            case SignInOutcome.RegistrationConflict:
                throw new ConflictApiException(
                    "报名状态已变化，请刷新后重试。");
            case SignInOutcome.DuplicateAttendance:
                throw new ConflictApiException(
                    "该报名已签到，不能重复签到。");
        }

        return new AttendanceResponse
        {
            AttendId = result.AttendId,
            RegId = context.RegId,
            SigninType = signinType,
            SignedInAt = signInAt,
            LatenessMinutes = latenessMinutes,
            DeductHours = deductHours,
            Remark = remark,
            CreatedAt = DateTime.Now
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
}
