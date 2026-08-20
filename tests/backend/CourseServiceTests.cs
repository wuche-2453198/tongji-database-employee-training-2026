using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

internal static class CourseServiceTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Course create defaults to DRAFT", CreateDefaultsToDraftAsync);
        yield return ("Course PUT carries the previously read status", UpdateCarriesExpectedStatusAsync);
        yield return ("Course PUT reports a concurrent status change", UpdateConflictAsync);
        yield return ("Published course freezes trainer", PublishedCourseFreezesTrainerAsync);
        yield return ("Started course freezes location", StartedCourseFreezesLocationAsync);
        yield return ("Close uses PUBLISHED to CLOSED conditional transition", CloseUsesConditionalTransitionAsync);
        yield return ("Close reports a concurrent transition", CloseConflictAsync);
        yield return ("Publish requires location", PublishRequiresLocationAsync);
        yield return ("Publish remains blocked without budget transaction", PublishRemainsBlockedAsync);
        yield return ("Capacity remaining seats never becomes negative", CapacityClampsRemainingSeatsAsync);
        yield return ("Missing course capacity remains null", MissingCapacityReturnsNullAsync);
    }

    private static async Task CreateDefaultsToDraftAsync()
    {
        var courseRepository = new FakeCourseRepository();
        var trainerRepository = new FakeTrainerRepository();
        var service = new CourseService(
            courseRepository,
            trainerRepository);

        var response = await service.CreateAsync(
            ValidCreateRequest(),
            CancellationToken.None);

        TestAssert.Equal(
            "DRAFT",
            response.CourseStatus,
            "A newly created course must be a draft.");
    }

    private static async Task UpdateCarriesExpectedStatusAsync()
    {
        var oldCourse = ValidCourse("DRAFT");
        var courseRepository = new FakeCourseRepository
        {
            Course = oldCourse
        };
        var service = CreateService(courseRepository);

        await service.UpdateAsync(
            oldCourse.CourseId,
            ValidUpdateRequest(oldCourse),
            CancellationToken.None);

        TestAssert.Equal(
            "DRAFT",
            courseRepository.LastUpdateExpectedStatus,
            "Course PUT must use the status read before validation.");
    }

    private static async Task UpdateConflictAsync()
    {
        var oldCourse = ValidCourse("DRAFT");
        var courseRepository = new FakeCourseRepository
        {
            Course = oldCourse,
            UpdateResult = false
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.UpdateAsync(
                oldCourse.CourseId,
                ValidUpdateRequest(oldCourse),
                CancellationToken.None),
            "A lost conditional update must be reported as a conflict.");
    }

    private static async Task PublishedCourseFreezesTrainerAsync()
    {
        var oldCourse = ValidCourse("PUBLISHED");
        var request = ValidUpdateRequest(oldCourse);
        request.TrainerId++;

        var courseRepository = new FakeCourseRepository
        {
            Course = oldCourse
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.UpdateAsync(
                oldCourse.CourseId,
                request,
                CancellationToken.None),
            "A published course must not change its trainer.");

        TestAssert.Equal(
            0,
            courseRepository.UpdateCallCount,
            "Rejected published-course edits must not reach the repository.");
    }

    private static async Task StartedCourseFreezesLocationAsync()
    {
        var oldCourse = ValidCourse(
            "PUBLISHED",
            DateTime.Now.AddHours(-2));
        var request = ValidUpdateRequest(oldCourse);
        request.Location = "新地点";

        var courseRepository = new FakeCourseRepository
        {
            Course = oldCourse
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.UpdateAsync(
                oldCourse.CourseId,
                request,
                CancellationToken.None),
            "A started course must not change location.");
    }

    private static async Task CloseUsesConditionalTransitionAsync()
    {
        var course = ValidCourse("PUBLISHED");
        var courseRepository = new FakeCourseRepository
        {
            Course = course
        };
        var service = CreateService(courseRepository);

        await service.CloseAsync(
            course.CourseId,
            CancellationToken.None);

        TestAssert.Equal(
            "PUBLISHED",
            courseRepository.LastStatusExpectedStatus,
            "Close must require the published state.");

        TestAssert.Equal(
            "CLOSED",
            courseRepository.LastStatusNewStatus,
            "Close must transition to the closed state.");
    }

    private static async Task CloseConflictAsync()
    {
        var course = ValidCourse("PUBLISHED");
        var courseRepository = new FakeCourseRepository
        {
            Course = course,
            StatusUpdateResult = false
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.CloseAsync(
                course.CourseId,
                CancellationToken.None),
            "A lost close transition must be reported as a conflict.");
    }

    private static async Task PublishRequiresLocationAsync()
    {
        var course = ValidCourse("DRAFT");
        course = CopyCourseWithLocation(
            course,
            null);

        var courseRepository = new FakeCourseRepository
        {
            Course = course
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.PublishAsync(
                course.CourseId,
                CancellationToken.None),
            "Publish must reject an incomplete location.");
    }

    private static async Task PublishRemainsBlockedAsync()
    {
        var course = ValidCourse("DRAFT");
        var courseRepository = new FakeCourseRepository
        {
            Course = course
        };
        var service = CreateService(courseRepository);

        await TestAssert.ThrowsAsync<ConflictApiException>(
            () => service.PublishAsync(
                course.CourseId,
                CancellationToken.None),
            "Publish must remain blocked without an atomic budget capability.");

        TestAssert.True(
            courseRepository.LastStatusNewStatus is null,
            "Blocked publish must not update the course status.");
    }

    private static async Task CapacityClampsRemainingSeatsAsync()
    {
        var courseRepository = new FakeCourseRepository
        {
            Capacity = (10, 12)
        };
        var service = CreateService(courseRepository);

        var capacity = await service.GetCapacityAsync(
            1,
            CancellationToken.None);

        TestAssert.True(
            capacity.HasValue,
            "Existing capacity data must produce a summary.");

        var capacityValue = capacity
            ?? throw new InvalidOperationException(
                "Existing capacity data must produce a summary.");

        TestAssert.Equal(
            0,
            capacityValue.RemainingSeats,
            "Remaining seats must not be negative.");
    }

    private static async Task MissingCapacityReturnsNullAsync()
    {
        var service = CreateService(
            new FakeCourseRepository());

        var capacity = await service.GetCapacityAsync(
            999,
            CancellationToken.None);

        TestAssert.True(
            capacity is null,
            "Missing capacity data must remain null until the public contract is confirmed.");
    }

    private static CourseService CreateService(
        FakeCourseRepository courseRepository)
    {
        return new CourseService(
            courseRepository,
            new FakeTrainerRepository());
    }

    private static TrainingCourse ValidCourse(
        string status,
        DateTime? startAt = null)
    {
        var start = startAt
            ?? DateTime.Now.AddDays(5);

        return new TrainingCourse
        {
            CourseId = 1,
            CourseName = "数据库并发控制",
            CourseType = "技术培训",
            DurationHours = 4m,
            TrainerId = 2,
            TrainerName = "测试讲师",
            MaxStudents = 20,
            StartAt = start,
            EndAt = start.AddHours(4),
            Location = "培训室 A",
            CourseStatus = status,
            BudgetAmount = 3000m,
            DeptId = 3,
            DeptName = "研发部",
            CreatedAt = DateTime.Now.AddDays(-1)
        };
    }

    private static CreateCourseRequest ValidCreateRequest()
    {
        var course = ValidCourse("DRAFT");

        return new CreateCourseRequest
        {
            CourseName = course.CourseName,
            CourseType = course.CourseType,
            DurationHours = course.DurationHours,
            TrainerId = course.TrainerId!.Value,
            MaxStudents = course.MaxStudents,
            StartAt = course.StartAt!.Value,
            EndAt = course.EndAt!.Value,
            Location = course.Location!,
            BudgetAmount = course.BudgetAmount,
            DeptId = course.DeptId!.Value
        };
    }

    private static UpdateCourseRequest ValidUpdateRequest(
        TrainingCourse course)
    {
        return new UpdateCourseRequest
        {
            CourseName = course.CourseName,
            CourseType = course.CourseType,
            DurationHours = course.DurationHours,
            TrainerId = course.TrainerId!.Value,
            MaxStudents = course.MaxStudents,
            StartAt = course.StartAt!.Value,
            EndAt = course.EndAt!.Value,
            Location = course.Location!,
            BudgetAmount = course.BudgetAmount,
            DeptId = course.DeptId!.Value,
            PreTestUrl = course.PreTestUrl,
            PostTestUrl = course.PostTestUrl,
            MaterialUrl = course.MaterialUrl
        };
    }

    private static TrainingCourse CopyCourseWithLocation(
        TrainingCourse source,
        string? location)
    {
        return new TrainingCourse
        {
            CourseId = source.CourseId,
            CourseName = source.CourseName,
            CourseType = source.CourseType,
            DurationHours = source.DurationHours,
            TrainerId = source.TrainerId,
            TrainerName = source.TrainerName,
            MaxStudents = source.MaxStudents,
            StartAt = source.StartAt,
            EndAt = source.EndAt,
            Location = location,
            CourseStatus = source.CourseStatus,
            BudgetAmount = source.BudgetAmount,
            DeptId = source.DeptId,
            DeptName = source.DeptName,
            CreatedAt = source.CreatedAt
        };
    }
}
