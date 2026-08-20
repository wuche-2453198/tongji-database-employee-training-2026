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
