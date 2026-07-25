using TrainingManagement.Api.Dtos.Trainer;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITrainerService
{
    Task<IReadOnlyCollection<TrainerResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<TrainerResponse?> GetByIdAsync(
        long trainerId,
        CancellationToken cancellationToken);

    Task<TrainerResponse> CreateAsync(
        CreateTrainerRequest request,
        CancellationToken cancellationToken);

    Task<TrainerResponse> UpdateAsync(
        long trainerId,
        UpdateTrainerRequest request,
        CancellationToken cancellationToken);
}