using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Enums;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Controllers;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.ModuleTests;

internal static class CourseControllerTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Employee course list is forced to published", EmployeeListForcesPublishedAsync);
        yield return ("HR course list preserves the requested status", HrListPreservesStatusAsync);
        yield return ("Employee cannot read a draft course", EmployeeRejectsDraftAsync);
        yield return ("Employee can read a published course", EmployeeReadsPublishedAsync);
        yield return ("HR can read a draft course", HrReadsDraftAsync);
    }

    private static async Task EmployeeListForcesPublishedAsync()
    {
        var service = new FakeCourseService();
        var controller = CreateController(service, TestPrincipals.Employee(55));

        await controller.GetAll(new CourseQuery(), CancellationToken.None);

        TestAssert.True(service.LastQuery is not null, "List must invoke the course service.");
        TestAssert.Equal(
            CourseStatusText.Published,
            service.LastQuery!.CourseStatus,
            "Employees must only see published courses.");
    }

    private static async Task HrListPreservesStatusAsync()
    {
        var service = new FakeCourseService();
        var controller = CreateController(service, TestPrincipals.Hr());

        await controller.GetAll(
            new CourseQuery { CourseStatus = CourseStatusText.Draft },
            CancellationToken.None);

        TestAssert.Equal(
            CourseStatusText.Draft,
            service.LastQuery!.CourseStatus,
            "HR must be able to request drafts.");
    }

    private static async Task EmployeeRejectsDraftAsync()
    {
        var service = new FakeCourseService
        {
            Course = Course(CourseStatusText.Draft)
        };
        var controller = CreateController(service, TestPrincipals.Employee(55));

        await TestAssert.ThrowsAsync<ForbiddenApiException>(
            () => controller.GetById(1, CancellationToken.None),
            "An employee must not read a draft course.");
    }

    private static async Task EmployeeReadsPublishedAsync()
    {
        var service = new FakeCourseService
        {
            Course = Course(CourseStatusText.Published)
        };
        var controller = CreateController(service, TestPrincipals.Employee(55));

        var result = await controller.GetById(1, CancellationToken.None);

        TestAssert.True(
            result.Result is OkObjectResult,
            "An employee must read a published course.");
    }

    private static async Task HrReadsDraftAsync()
    {
        var service = new FakeCourseService
        {
            Course = Course(CourseStatusText.Draft)
        };
        var controller = CreateController(service, TestPrincipals.Hr());

        var result = await controller.GetById(1, CancellationToken.None);

        TestAssert.True(
            result.Result is OkObjectResult,
            "HR must read a draft course.");
    }

    private static CoursesController CreateController(
        ICourseService service,
        System.Security.Claims.ClaimsPrincipal principal)
    {
        return new CoursesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static CourseResponse Course(string status)
    {
        return new CourseResponse
        {
            CourseId = 1,
            CourseName = "数据库并发控制",
            CourseType = "技术培训",
            MaxStudents = 20,
            CourseStatus = status
        };
    }

    private sealed class FakeCourseService : ICourseService
    {
        public CourseQuery? LastQuery { get; private set; }

        public CourseResponse? Course { get; set; }

        public Task<PagedResult<CourseResponse>> GetAllAsync(
            CourseQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(
                new PagedResult<CourseResponse>(
                    Array.Empty<CourseResponse>(),
                    query.Page,
                    query.PageSize,
                    0));
        }

        public Task<CourseResponse?> GetByIdAsync(
            long courseId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Course);
        }

        public Task<CourseResponse> CreateAsync(
            CreateCourseRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CourseResponse> UpdateAsync(
            long courseId,
            UpdateCourseRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<(int MaxStudents, int ValidRegistrationCount, int RemainingSeats)?> GetCapacityAsync(
            long courseId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PublishCourseResponse> PublishAsync(
            long courseId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task CloseAsync(
            long courseId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
