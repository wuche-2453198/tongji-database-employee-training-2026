using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Implementations;

namespace TrainingManagement.Api.ModuleTests;

internal static class TrainerServiceTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("Trainer PUT normalizes writable fields", UpdateNormalizesFieldsAsync);
        yield return ("Trainer PUT returns 404 for a missing trainer", UpdateMissingTrainerAsync);
        yield return ("Trainer PUT rejects star level outside 1 to 5", UpdateRejectsStarLevelAsync);
        yield return ("Trainer PUT rejects invalid internal flag", UpdateRejectsInternalFlagAsync);
        yield return ("Trainer query trims and normalizes filters", QueryNormalizesFiltersAsync);
        yield return ("Trainer query preserves pagination metadata", QueryPreservesPaginationAsync);
        yield return ("Trainer query filters and pages without losing total", FilteredPagesAsync);
        yield return ("Trainer PUT detects a deleted row", DeletedTrainerAsync);
        yield return ("Trainer create DTO defaults to 3 stars", CreateRequestDefaultsToThreeStarsAsync);
    }

    private static async Task UpdateNormalizesFieldsAsync()
    {
        var repository = new FakeTrainerRepository
        {
            Trainer = ValidTrainer()
        };
        var service = new TrainerService(repository);

        var response = await service.UpdateAsync(
            1,
            new UpdateTrainerRequest
            {
                TrainerName = "  李老师  ",
                Title = "  高级讲师  ",
                Company = "  同济大学  ",
                Phone = "  13800000000  ",
                Email = "  teacher@example.com  ",
                StarLevel = 4.5m,
                IsInternal = " n "
            },
            CancellationToken.None);

        TestAssert.Equal(
            "李老师",
            response.TrainerName,
            "Trainer name must be trimmed.");

        TestAssert.Equal(
            "高级讲师",
            repository.LastUpdatedTrainer?.Title,
            "Trainer title must be trimmed.");

        TestAssert.Equal(
            "N",
            response.IsInternal,
            "Internal flag must be normalized to uppercase.");
    }

    private static async Task UpdateMissingTrainerAsync()
    {
        var service = new TrainerService(
            new FakeTrainerRepository());

        await TestAssert.ThrowsAsync<NotFoundApiException>(
            () => service.UpdateAsync(
                999,
                ValidUpdateRequest(),
                CancellationToken.None),
            "Updating a missing trainer must return not found.");
    }

    private static async Task UpdateRejectsStarLevelAsync()
    {
        var repository = new FakeTrainerRepository
        {
            Trainer = ValidTrainer()
        };
        var service = new TrainerService(repository);
        var request = ValidUpdateRequest();
        request.StarLevel = 5.1m;

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.UpdateAsync(
                1,
                request,
                CancellationToken.None),
            "Trainer PUT must reject a star level above 5.");
    }

    private static async Task UpdateRejectsInternalFlagAsync()
    {
        var repository = new FakeTrainerRepository
        {
            Trainer = ValidTrainer()
        };
        var service = new TrainerService(repository);
        var request = ValidUpdateRequest();
        request.IsInternal = "YES";

        await TestAssert.ThrowsAsync<BusinessException>(
            () => service.UpdateAsync(
                1,
                request,
                CancellationToken.None),
            "Trainer PUT must reject flags other than Y or N.");
    }

    private static async Task QueryNormalizesFiltersAsync()
    {
        var repository = new FakeTrainerRepository();
        var service = new TrainerService(repository);

        await service.GetAllAsync(
            new TrainerQuery
            {
                TrainerName = "  李老师  ",
                Company = "  同济大学  ",
                IsInternal = " n "
            },
            CancellationToken.None);

        TestAssert.Equal(
            "李老师",
            repository.LastQuery?.TrainerName,
            "Trainer-name filter must be trimmed.");

        TestAssert.Equal(
            "同济大学",
            repository.LastQuery?.Company,
            "Company filter must be trimmed.");

        TestAssert.Equal(
            "N",
            repository.LastQuery?.IsInternal,
            "Internal filter must be normalized to uppercase.");
    }

    private static async Task QueryPreservesPaginationAsync()
    {
        var repository = new FakeTrainerRepository
        {
            Trainer = ValidTrainer()
        };
        var service = new TrainerService(repository);

        var result = await service.GetAllAsync(
            new TrainerQuery
            {
                Page = 3,
                PageSize = 7
            },
            CancellationToken.None);

        TestAssert.Equal(3, result.Page, "Trainer page must be preserved.");
        TestAssert.Equal(7, result.PageSize, "Trainer page size must be preserved.");
        TestAssert.Equal(1L, result.Total, "Trainer total must come from repository.");
        TestAssert.Equal(3, repository.LastQuery?.Page, "Repository must receive the page.");
        TestAssert.Equal(7, repository.LastQuery?.PageSize, "Repository must receive the page size.");
        TestAssert.Equal(0, result.Items.Count, "Page beyond total must be empty.");
    }

    private static async Task FilteredPagesAsync()
    {
        var rows = Enumerable.Range(1, 6).Select(i => new Trainer
        {
            TrainerId = i, TrainerName = "李老师" + i,
            Company = i == 6 ? "其他" : "同济大学", IsInternal = i == 5 ? "N" : "Y"
        }).ToArray();
        var service = new TrainerService(new FakeTrainerRepository { Trainers = rows });
        var result = await service.GetAllAsync(new TrainerQuery
        {
            TrainerName = " 李老师 ", Company = " 同济 ", IsInternal = " y ", Page = 2, PageSize = 2
        }, CancellationToken.None);
        TestAssert.Equal(4L, result.Total, "Total must count all matching rows.");
        TestAssert.True(result.Items.Select(t => t.TrainerId).SequenceEqual(new long[] { 2, 1 }), "Second page must retain descending order and filters.");
        var empty = await service.GetAllAsync(new TrainerQuery { TrainerName = "不存在" }, CancellationToken.None);
        TestAssert.Equal(0L, empty.Total, "No matches have zero total.");
        var large = await service.GetAllAsync(new TrainerQuery { Page = int.MaxValue, PageSize = 100 }, CancellationToken.None);
        TestAssert.Equal(0, large.Items.Count, "Large offset must not wrap to first page.");
        var defaults = await service.GetAllAsync(new TrainerQuery(), CancellationToken.None);
        TestAssert.Equal(1, defaults.Page, "Default page.");
        TestAssert.Equal(20, defaults.PageSize, "Default page size.");
    }

    private static async Task DeletedTrainerAsync()
    {
        var service = new TrainerService(new FakeTrainerRepository { Trainer = ValidTrainer(), UpdateResult = false });
        await TestAssert.ThrowsAsync<NotFoundApiException>(
            () => service.UpdateAsync(1, ValidUpdateRequest(), CancellationToken.None), "Deleted row must not report successful PUT.");
    }

    private static Task CreateRequestDefaultsToThreeStarsAsync()
    {
        var request = new CreateTrainerRequest();

        TestAssert.Equal(
            3.0m,
            request.StarLevel,
            "A new trainer request must default to three stars.");

        return Task.CompletedTask;
    }

    private static Trainer ValidTrainer()
    {
        return new Trainer
        {
            TrainerId = 1,
            TrainerName = "原讲师",
            Title = "讲师",
            Company = "同济大学",
            StarLevel = 3.0m,
            IsInternal = "Y",
            CreatedAt = DateTime.Now.AddDays(-1)
        };
    }

    private static UpdateTrainerRequest ValidUpdateRequest()
    {
        return new UpdateTrainerRequest
        {
            TrainerName = "李老师",
            Title = "高级讲师",
            Company = "同济大学",
            Phone = "13800000000",
            Email = "teacher@example.com",
            StarLevel = 4.5m,
            IsInternal = "Y"
        };
    }
}
