using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

internal static class RegistrationServiceTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Create requires a filed request", CreateRequiresFiledRequestAsync);
        yield return ("Create rejects a resigned employee", CreateRejectsResignedEmployeeAsync);
        yield return ("Create rejects an active blacklist", CreateRejectsActiveBlacklistAsync);
        yield return ("Create rejects an unpublished course", CreateRejectsUnpublishedCourseAsync);
        yield return ("Create rejects a started course", CreateRejectsStartedCourseAsync);
        yield return ("Create succeeds with a filed request", CreateSucceedsAsync);
        yield return ("Create reports a full course", CreateReportsCapacityFullAsync);
        yield return ("Create reports a duplicate registration", CreateReportsDuplicateAsync);
        yield return ("Cancel succeeds for the owner", CancelSucceedsForOwnerAsync);
        yield return ("Cancel forbids another employee", CancelForbidsOtherEmployeeAsync);
        yield return ("Cancel allows HR for any employee", CancelAllowsHrAsync);
        yield return ("Cancel rejects a started course", CancelRejectsStartedCourseAsync);
        yield return ("Cancel rejects a non-registered status", CancelRejectsNonRegisteredAsync);
        yield return ("Absent requires HR or admin", AbsentRequiresHrOrAdminAsync);
        yield return ("Absent succeeds after course end", AbsentSucceedsAfterCourseEndAsync);
        yield return ("Absent rejects before course end", AbsentRejectsBeforeCourseEndAsync);
        yield return ("Complete requires signed in", CompleteRequiresSignedInAsync);
        yield return ("Complete succeeds after course end", CompleteSucceedsAfterCourseEndAsync);
        yield return ("Complete rejects before course end", CompleteRejectsBeforeCourseEndAsync);
        yield return ("Detail hides other employee registrations", DetailHidesOtherEmployeeAsync);
        yield return ("My registrations scope to the current employee", MyRegistrationsScopeToEmployeeAsync);
        yield return ("Summary computes remaining seats", SummaryComputesRemainingSeatsAsync);
        yield return ("Summary rejects a missing course", SummaryRejectsMissingCourseAsync);
        yield return ("Summary requires HR or admin", SummaryRequiresHrOrAdminAsync);
    }

    private static async Task CreateRequiresFiledRequestAsync()
    {
        var repository = new FakeRegistrationRepository
        {
            FiledRequestId = null
        };
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Create must reject a missing HR_FILED request.");
    }

    private static async Task CreateRejectsResignedEmployeeAsync()
    {
        var repository = CreateEligibleRepository();
        repository.Employee = ValidEmployee("RESIGNED");
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Create must reject a resigned employee.");
    }

    private static async Task CreateRejectsActiveBlacklistAsync()
    {
        var repository = CreateEligibleRepository();
        repository.Employee = ValidEmployee(
            "ACTIVE",
            activeBlacklistCount: 1);
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Create must reject an employee with an active blacklist.");
    }

    private static async Task CreateRejectsUnpublishedCourseAsync()
    {
        var repository = CreateEligibleRepository();
        repository.Course = ValidCourse(
            "DRAFT",
            DateTime.Now.AddDays(5));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Create must reject a course that is not published.");
    }

    private static async Task CreateRejectsStartedCourseAsync()
    {
        var repository = CreateEligibleRepository();
        repository.Course = ValidCourse(
            "PUBLISHED",
            DateTime.Now.AddHours(-1));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Create must reject a course that has already started.");
    }

    private static async Task CreateSucceedsAsync()
    {
        var repository = CreateEligibleRepository();
        var service = CreateService(repository);

        var response = await service.CreateAsync(
            ValidCreateRequest(),
            TestPrincipals.Employee(10),
            CancellationToken.None);

        TestAssert.Equal(
            100L,
            response.RegId,
            "Created registration must return the new id.");

        TestAssert.Equal(
            "REGISTERED",
            response.Status,
            "Created registration must be REGISTERED.");

        TestAssert.Equal(
            10L,
            repository.LastCreated?.EmpId,
            "The registration must be derived from the current employee.");

        TestAssert.Equal(
            2L,
            repository.LastCreated?.CourseId,
            "The registration must carry the requested course.");
    }

    private static async Task CreateReportsCapacityFullAsync()
    {
        var repository = CreateEligibleRepository();
        repository.CreateResult = new RegistrationCreateResult(
            RegistrationCreateOutcome.CapacityFull);
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "A full course must be reported as a conflict.");
    }

    private static async Task CreateReportsDuplicateAsync()
    {
        var repository = CreateEligibleRepository();
        repository.CreateResult = new RegistrationCreateResult(
            RegistrationCreateOutcome.Duplicate);
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CreateAsync(
                ValidCreateRequest(),
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "A duplicate registration must be reported as a conflict.");
    }

    private static async Task CancelSucceedsForOwnerAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        var response = await service.CancelAsync(
            1,
            new CancelRegistrationRequest
            {
                Reason = "临时有事"
            },
            TestPrincipals.Employee(10),
            CancellationToken.None);

        TestAssert.Equal(
            "CANCELED",
            response.Status,
            "Cancel must transition REGISTERED to CANCELED.");

        TestAssert.Equal(
            "临时有事",
            repository.LastCancelReason,
            "Cancel must persist the optional reason.");
    }

    private static async Task CancelForbidsOtherEmployeeAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.CancelAsync(
                1,
                null,
                TestPrincipals.Employee(11),
                CancellationToken.None),
            "An employee must not cancel another employee's registration.");
    }

    private static async Task CancelAllowsHrAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        var response = await service.CancelAsync(
            1,
            null,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            "CANCELED",
            response.Status,
            "HR must be allowed to cancel any registration.");
    }

    private static async Task CancelRejectsStartedCourseAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(
                status: "REGISTERED",
                startAt: DateTime.Now.AddHours(-1)));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CancelAsync(
                1,
                null,
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Cancel must reject a course that has already started.");
    }

    private static async Task CancelRejectsNonRegisteredAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "SIGNED_IN"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CancelAsync(
                1,
                null,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Cancel must only be allowed from REGISTERED.");
    }

    private static async Task AbsentRequiresHrOrAdminAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.MarkAbsentAsync(
                1,
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Only HR or admin can mark absence.");
    }

    private static async Task AbsentSucceedsAfterCourseEndAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(
                status: "REGISTERED",
                endAt: DateTime.Now.AddHours(-1)));
        var service = CreateService(repository);

        var response = await service.MarkAbsentAsync(
            1,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            "ABSENT",
            response.Status,
            "Absent must transition REGISTERED to ABSENT.");
    }

    private static async Task AbsentRejectsBeforeCourseEndAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.MarkAbsentAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Absence must only be allowed after the course ends.");
    }

    private static async Task CompleteRequiresSignedInAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CompleteAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Complete must only be allowed from SIGNED_IN.");
    }

    private static async Task CompleteSucceedsAfterCourseEndAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(
                status: "SIGNED_IN",
                endAt: DateTime.Now.AddHours(-1)));
        var service = CreateService(repository);

        var response = await service.CompleteAsync(
            1,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            "COMPLETED",
            response.Status,
            "Complete must transition SIGNED_IN to COMPLETED.");
    }

    private static async Task CompleteRejectsBeforeCourseEndAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "SIGNED_IN"));
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CompleteAsync(
                1,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "Complete must only be allowed after the course ends.");
    }

    private static async Task DetailHidesOtherEmployeeAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        var hidden = await service.GetByIdAsync(
            1,
            TestPrincipals.Employee(11),
            CancellationToken.None);

        TestAssert.True(
            hidden is null,
            "A registration of another employee must be hidden from employees.");

        var visible = await service.GetByIdAsync(
            1,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.True(
            visible is not null,
            "HR must be able to read any registration.");
    }

    private static async Task MyRegistrationsScopeToEmployeeAsync()
    {
        var repository = CreateContextRepository(
            ValidContext(status: "REGISTERED"));
        var service = CreateService(repository);

        await service.GetMyAsync(
            new RegistrationQuery(),
            TestPrincipals.Employee(10),
            CancellationToken.None);

        TestAssert.Equal(
            10L,
            repository.LastScopeEmpId,
            "My registrations must be scoped to the current employee.");
    }

    private static async Task SummaryComputesRemainingSeatsAsync()
    {
        var repository = new FakeRegistrationRepository
        {
            Summary = new RegistrationSummaryRecord
            {
                Total = 5,
                Registered = 3,
                SignedIn = 1,
                Absent = 0,
                Completed = 1,
                Canceled = 1,
                MaxStudents = 10
            }
        };
        var service = CreateService(repository);

        var summary = await service.GetSummaryAsync(
            2,
            TestPrincipals.Hr(),
            CancellationToken.None);

        TestAssert.Equal(
            6L,
            summary.RemainingSeats,
            "Remaining seats must subtract only active registrations.");
    }

    private static async Task SummaryRejectsMissingCourseAsync()
    {
        var repository = new FakeRegistrationRepository
        {
            Summary = new RegistrationSummaryRecord
            {
                MaxStudents = null
            }
        };
        var service = CreateService(repository);

        await TestAssert.ThrowsAsync<NotFoundApiException>(
            () => service.GetSummaryAsync(
                999,
                TestPrincipals.Hr(),
                CancellationToken.None),
            "A summary for a missing course must be a 404.");
    }

    private static async Task SummaryRequiresHrOrAdminAsync()
    {
        var service = CreateService(
            new FakeRegistrationRepository());

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => service.GetSummaryAsync(
                null,
                TestPrincipals.Employee(10),
                CancellationToken.None),
            "Only HR or admin can read the registration summary.");
    }

    private static RegistrationService CreateService(
        FakeRegistrationRepository repository)
    {
        return new RegistrationService(repository);
    }

    private static CreateRegistrationRequest ValidCreateRequest()
    {
        return new CreateRegistrationRequest
        {
            CourseId = 2
        };
    }

    private static FakeRegistrationRepository CreateEligibleRepository()
    {
        return new FakeRegistrationRepository
        {
            FiledRequestId = 5,
            Employee = ValidEmployee("ACTIVE"),
            Course = ValidCourse(
                "PUBLISHED",
                DateTime.Now.AddDays(5)),
            Registration = ValidRegistration()
        };
    }

    private static FakeRegistrationRepository CreateContextRepository(
        RegistrationContextRecord context)
    {
        return new FakeRegistrationRepository
        {
            Context = context,
            Registration = ValidRegistration(context)
        };
    }

    private static EmployeeEligibilityRecord ValidEmployee(
        string status,
        int activeBlacklistCount = 0)
    {
        return new EmployeeEligibilityRecord
        {
            EmpId = 10,
            Status = status,
            ActiveBlacklistCount = activeBlacklistCount
        };
    }

    private static CourseEligibilityRecord ValidCourse(
        string courseStatus,
        DateTime startAt)
    {
        return new CourseEligibilityRecord
        {
            CourseId = 2,
            CourseStatus = courseStatus,
            StartAt = startAt,
            EndAt = startAt.AddHours(4),
            DurationHours = 4m,
            MaxStudents = 20
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

    private static RegistrationRecord ValidRegistration(
        RegistrationContextRecord? context = null)
    {
        var ctx = context
            ?? ValidContext("REGISTERED");

        return new RegistrationRecord
        {
            RegId = ctx.RegId,
            RequestId = ctx.RequestId,
            EmpId = ctx.EmpId,
            EmpName = "测试员工",
            DeptId = 3,
            DeptName = "研发部",
            CourseId = ctx.CourseId,
            CourseName = "数据库并发控制",
            CourseType = "技术培训",
            DurationHours = ctx.DurationHours,
            StartAt = ctx.StartAt,
            EndAt = ctx.EndAt,
            Location = "培训室 A",
            CourseStatus = ctx.CourseStatus,
            MaxStudents = ctx.MaxStudents,
            Status = ctx.Status,
            RegisteredAt = DateTime.Now.AddDays(-2),
            ActualHours = ctx.ActualHours,
            UpdatedAt = DateTime.Now
        };
    }
}
