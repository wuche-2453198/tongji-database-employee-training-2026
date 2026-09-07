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
        yield return ("Course create and PUT reject missing departments", MissingDepartmentAsync);
        yield return ("Published course freezes every protected field", FrozenFieldsAsync);
        yield return ("Course terminal and missing states reject writes", InvalidStatesAsync);
        yield return ("Capacity reports empty partial and full courses", CapacityCountsAsync);
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
        yield return ("Course create and PUT reject Oracle numeric overflow", RejectsNumericOverflowAsync);
        yield return ("Published course rejects blank location", PublishedRejectsBlankLocationAsync);
        yield return ("Draft blank and future published nonblank locations remain editable", ValidLocationUpdatesAsync);
    }

    private static async Task RejectsNumericOverflowAsync()
    {
        foreach (var values in new[] { (1000m, 30), (4m, 1000000), (0.01m, 30) })
        {
            var repository = new FakeCourseRepository { Course = ValidCourse("DRAFT") };
            var service = CreateService(repository);
            var create = ValidCreateRequest();
            create.DurationHours = values.Item1;
            create.MaxStudents = values.Item2;
            await TestAssert.ThrowsAsync<BusinessException>(
                () => service.CreateAsync(create, CancellationToken.None), "Reject invalid create numeric bounds.");
            var update = ValidUpdateRequest(repository.Course!);
            update.DurationHours = values.Item1;
            update.MaxStudents = values.Item2;
            await TestAssert.ThrowsAsync<BusinessException>(
                () => service.UpdateAsync(1, update, CancellationToken.None), "Reject invalid update numeric bounds.");
            TestAssert.Equal(0, repository.UpdateCallCount, "Invalid edit must not write.");
        }
    }

    private static async Task MissingDepartmentAsync()
    {
        var repo = new FakeCourseRepository { Course = ValidCourse("DRAFT"), DepartmentExistsResult = false };
        var service = CreateService(repo);
        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(ValidCreateRequest(), CancellationToken.None), "Unknown department must fail before insert.");
        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.UpdateAsync(1, ValidUpdateRequest(repo.Course!), CancellationToken.None), "Unknown department must fail before update.");
        TestAssert.Equal(0, repo.UpdateCallCount, "Unknown department must not write.");
    }

    private static async Task FrozenFieldsAsync()
    {
        foreach (var mutate in new Action<UpdateCourseRequest>[]
        {
            r => r.CourseName += "changed", r => r.CourseType = "管理培训",
            r => r.TrainerId++, r => r.DeptId++, r => r.StartAt = r.StartAt.AddMinutes(1),
            r => r.EndAt = r.EndAt.AddMinutes(1), r => r.MaxStudents++,
            r => r.DurationHours++, r => r.BudgetAmount++
        })
        {
            var repo = new FakeCourseRepository { Course = ValidCourse("PUBLISHED") };
            var request = ValidUpdateRequest(repo.Course);
            mutate(request);
            await TestAssert.ThrowsAsync<BusinessException>(
                () => CreateService(repo).UpdateAsync(1, request, CancellationToken.None), "Frozen field must reject edit.");
            TestAssert.Equal(0, repo.UpdateCallCount, "Frozen field edit must not write.");
        }
    }

    private static async Task InvalidStatesAsync()
    {
        foreach (var status in new[] { "DRAFT", "CLOSED" })
        {
            var repo = new FakeCourseRepository { Course = ValidCourse(status) };
            await TestAssert.ThrowsAsync<BusinessException>(
                () => CreateService(repo).CloseAsync(1, CancellationToken.None), "Only PUBLISHED may close.");
            TestAssert.True(repo.LastStatusNewStatus is null, "Rejected close must not write.");
        }
        var closed = new FakeCourseRepository { Course = ValidCourse("CLOSED") };
        await TestAssert.ThrowsAsync<BusinessException>(
            () => CreateService(closed).UpdateAsync(1, ValidUpdateRequest(closed.Course), CancellationToken.None), "CLOSED is terminal.");
        var missing = CreateService(new FakeCourseRepository());
        await TestAssert.ThrowsAsync<NotFoundApiException>(() => missing.CloseAsync(999, CancellationToken.None), "Missing close is 404.");
        await TestAssert.ThrowsAsync<NotFoundApiException>(() => missing.PublishAsync(999, CancellationToken.None), "Missing publish is 404.");
    }

    private static async Task CapacityCountsAsync()
    {
        foreach (var count in new[] { 0, 7, 20 })
        {
            var repo = new FakeCourseRepository { Capacity = (20, count) };
            var result = await CreateService(repo).GetCapacityAsync(1, CancellationToken.None);
            TestAssert.Equal(20 - count, result!.Value.RemainingSeats, "Capacity subtraction must be exact.");
            TestAssert.Equal(count, result.Value.ValidRegistrationCount, "Retain valid count.");
        }
    }

    private static async Task PublishedRejectsBlankLocationAsync()
    {
        var course = ValidCourse("PUBLISHED");
        var repository = new FakeCourseRepository { Course = course };
        var request = ValidUpdateRequest(course);
        request.Location = "   ";
        await TestAssert.ThrowsAsync<BusinessException>(
            () => CreateService(repository).UpdateAsync(course.CourseId, request, CancellationToken.None),
            "Published course must retain a location required by the database constraint.");
        TestAssert.Equal(0, repository.UpdateCallCount, "Blank published location must not write.");
    }

    private static async Task ValidLocationUpdatesAsync()
    {
        foreach (var values in new[] { ("DRAFT", "   "), ("PUBLISHED", "培训室 B") })
        {
            var course = ValidCourse(values.Item1);
            var repository = new FakeCourseRepository { Course = course };
            var request = ValidUpdateRequest(course);
            request.Location = values.Item2;
            await CreateService(repository).UpdateAsync(course.CourseId, request, CancellationToken.None);
            TestAssert.Equal(1, repository.UpdateCallCount, "Valid location edit must be written.");
        }
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
