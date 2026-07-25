using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;

public sealed class TrainerService : ITrainerService
{
    private readonly ITrainerRepository _trainerRepository;

    public TrainerService(ITrainerRepository trainerRepository)
    {
        _trainerRepository = trainerRepository;
    }

    public async Task<IReadOnlyCollection<TrainerResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var trainers = await _trainerRepository.GetAllAsync(cancellationToken);
        return trainers.Select(ToResponse).ToArray();
    }

    public async Task<TrainerResponse?> GetByIdAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        var trainer = await _trainerRepository.GetByIdAsync(trainerId, cancellationToken);
        return trainer is null ? null : ToResponse(trainer);
    }

    public async Task<TrainerResponse> CreateAsync(
        CreateTrainerRequest request,
        CancellationToken cancellationToken)
    {
        CheckTrainerRequest(request.TrainerName, request.StarLevel, request.IsInternal);

        var trainer = new Trainer
        {
            TrainerName = request.TrainerName.Trim(),
            Title = TrimToNull(request.Title),
            Company = TrimToNull(request.Company),
            Phone = TrimToNull(request.Phone),
            Email = TrimToNull(request.Email),
            StarLevel = request.StarLevel,
            IsInternal = request.IsInternal.Trim().ToUpperInvariant()
        };

        var newId = await _trainerRepository.CreateAsync(trainer, cancellationToken);
        var created = await _trainerRepository.GetByIdAsync(newId, cancellationToken);

        if (created is null)
        {
            throw new BusinessException("讲师创建失败。");
        }

        return ToResponse(created);
    }

    public async Task<TrainerResponse> UpdateAsync(
        long trainerId,
        UpdateTrainerRequest request,
        CancellationToken cancellationToken)
    {
        CheckTrainerRequest(request.TrainerName, request.StarLevel, request.IsInternal);

        var exists = await _trainerRepository.ExistsAsync(trainerId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundApiException("讲师不存在。");
        }

        var trainer = new Trainer
        {
            TrainerId = trainerId,
            TrainerName = request.TrainerName.Trim(),
            Title = TrimToNull(request.Title),
            Company = TrimToNull(request.Company),
            Phone = TrimToNull(request.Phone),
            Email = TrimToNull(request.Email),
            StarLevel = request.StarLevel,
            IsInternal = request.IsInternal.Trim().ToUpperInvariant()
        };

        var success = await _trainerRepository.UpdateAsync(trainer, cancellationToken);
        if (!success)
        {
            throw new NotFoundApiException("讲师不存在或已被删除。");
        }

        var updated = await _trainerRepository.GetByIdAsync(trainerId, cancellationToken);
        if (updated is null)
        {
            throw new NotFoundApiException("讲师不存在。");
        }

        return ToResponse(updated);
    }

    private static void CheckTrainerRequest(
        string trainerName,
        decimal starLevel,
        string isInternal)
    {
        if (string.IsNullOrWhiteSpace(trainerName))
        {
            throw new BusinessException("讲师姓名不能为空。");
        }

        if (starLevel < 1.0m || starLevel > 5.0m)
        {
            throw new BusinessException("讲师星级必须在1.0到5.0之间。");
        }

        if (decimal.Round(starLevel, 1) != starLevel)
        {
            throw new BusinessException("讲师星级最多保留一位小数。");
        }

        var internalFlag = isInternal.Trim().ToUpperInvariant();
        if (internalFlag is not ("Y" or "N"))
        {
            throw new BusinessException("是否内部讲师只能填写Y或N。");
        }
    }

    private static TrainerResponse ToResponse(Trainer trainer)
    {
        return new TrainerResponse
        {
            TrainerId = trainer.TrainerId,
            TrainerName = trainer.TrainerName,
            Title = trainer.Title,
            Company = trainer.Company,
            Phone = trainer.Phone,
            Email = trainer.Email,
            StarLevel = trainer.StarLevel,
            IsInternal = trainer.IsInternal,
            CreatedAt = trainer.CreatedAt,
            UpdatedAt = trainer.UpdatedAt
        };
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}