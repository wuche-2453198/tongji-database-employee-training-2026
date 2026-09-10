using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Attendance;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

internal static class AttendanceServiceTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Sign in succeeds with full on-time hours", SignInSucceedsAsync);
        yield return ("Sign in requires HR or admin", SignInRequiresHrOrAdminAsync);
        yield return ("Sign in rejects a non-registered status", SignInRejectsNonRegisteredAsync);
        yield return ("Sign in rejects after course end", SignInRejectsAfterCourseEndAsync);
        yield return ("Sign in reports a duplicate attendance", SignInReportsDuplicateAsync);
        yield return ("Manual sign in requires a remark", ManualRequiresRemarkAsync);
        yield return ("Manual sign in rejects a future time", ManualRejectsFutureTimeAsync);
        yield return ("Manual sign in rejects too-early time", ManualRejectsTooEarlyAsync);
        yield return ("Manual sign in computes lateness deduction", ManualComputesDeductionAsync);
    }

    private static async Task SignInSucceedsAsync()
    {
        var attendanceRepository = new FakeAttendanceRepository();
        var registrationRepository = CreateRegistrationRepository(
            ValidContext("REGISTERED"));
        var service = CreateService(
            attendanceRepository,
            registrationRepository);

        var response = await service.SignInAsync(
            1,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            "SCAN",
            response.SigninType,
            "Normal sign in must use the SCAN type.");

        TestAssert.Equal(
            0,
            response.LatenessMinutes,
            "An on-time sign in must not be late.");

        TestAssert.Equal(
            4m,
            attendanceRepository.LastActualHours,
            "An on-time sign in must keep the full course hours.");
    }

    private static async Task SignInRequiresHrOrAdminAsync()
    {
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext("REGISTERED")));

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.SignInAsync(
                1,
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Only HR or admin can sign in.");
    }

    private static async Task SignInRejectsNonRegisteredAsync()
    {
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext("SIGNED_IN")));

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.SignInAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Sign in must only be allowed from REGISTERED.");
    }

    private static async Task SignInRejectsAfterCourseEndAsync()
    {
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext(
                    "REGISTERED",
                    endAt: DateTime.Now.AddHours(-1))));

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.SignInAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Sign in must be rejected after the course ends.");
    }

    private static async Task SignInReportsDuplicateAsync()
    {
        var attendanceRepository = new FakeAttendanceRepository
        {
            Result = new SignInResult(
                SignInOutcome.DuplicateAttendance)
        };
        var service = CreateService(
            attendanceRepository,
            CreateRegistrationRepository(
                ValidContext("REGISTERED")));

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.SignInAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "A duplicate attendance must be reported as a conflict.");
    }

    private static async Task ManualRequiresRemarkAsync()
    {
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext("REGISTERED")));

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.ManualSignInAsync(
                new CreateManualAttendanceRequest
                {
                    RegId = 1,
                    Remark = "  "
                },
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Manual sign in must require a non-empty remark.");
    }

    private static async Task ManualRejectsFutureTimeAsync()
    {
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext("REGISTERED")));

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.ManualSignInAsync(
                new CreateManualAttendanceRequest
                {
                    RegId = 1,
                    SigninTime = DateTime.Now.AddHours(1),
                    Remark = "补签"
                },
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Manual sign in must reject a future time.");
    }

    private static async Task ManualRejectsTooEarlyAsync()
    {
        var startAt = DateTime.Now.AddHours(1);
        var service = CreateService(
            new FakeAttendanceRepository(),
            CreateRegistrationRepository(
                ValidContext(
                    "REGISTERED",
                    startAt: startAt,
                    endAt: startAt.AddHours(4))));

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.ManualSignInAsync(
                new CreateManualAttendanceRequest
                {
                    RegId = 1,
                    SigninTime = DateTime.Now.AddHours(-1),
                    Remark = "补签"
                },
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Manual sign in must reject a time before the course window.");
    }

    private static async Task ManualComputesDeductionAsync()
    {
        var startAt = DateTime.Now.AddHours(-4);
        var attendanceRepository = new FakeAttendanceRepository();
        var service = CreateService(
            attendanceRepository,
            CreateRegistrationRepository(
                ValidContext(
                    "REGISTERED",
                    startAt: startAt,
                    endAt: startAt.AddHours(4))));

        var response = await service.ManualSignInAsync(
            new CreateManualAttendanceRequest
            {
                RegId = 1,
                SigninTime = startAt.AddMinutes(30),
                Remark = "会议冲突，晚到半小时"
            },
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            "MANUAL",
            response.SigninType,
            "Manual sign in must use the MANUAL type.");

        TestAssert.Equal(
            30,
            response.LatenessMinutes,
            "Lateness must equal the minutes after course start.");

        TestAssert.Equal(
            2m,
            response.DeductHours,
            "30 minutes late on a 4-hour course must deduct 2 hours.");

        TestAssert.Equal(
            2m,
            attendanceRepository.LastActualHours,
            "Actual hours must be the duration minus the deduction.");
    }

    private static AttendanceService CreateService(
        FakeAttendanceRepository attendanceRepository,
        FakeRegistrationRepository registrationRepository)
    {
        return new AttendanceService(
            attendanceRepository,
            registrationRepository);
    }

    private static FakeRegistrationRepository CreateRegistrationRepository(
        RegistrationContextRecord context)
    {
        return new FakeRegistrationRepository
        {
            Context = context
        };
    }

    private static RegistrationContextRecord ValidContext(
        string status,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        var start = startAt
            ?? DateTime.Now.AddDays(5);

        return new RegistrationContextRecord
        {
            RegId = 1,
            RequestId = 5,
            EmpId = 10,
            CourseId = 2,
            Status = status,
            ActualHours = status == "SIGNED_IN" ? 4m : null,
            CourseStatus = "PUBLISHED",
            StartAt = start,
            EndAt = endAt ?? start.AddHours(4),
            DurationHours = 4m,
            MaxStudents = 20
        };
    }
}
