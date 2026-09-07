using TrainingManagement.Api.Dtos.Trainer;

using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITrainerService
{
    Task<PagedResult<TrainerResponse>> GetAllAsync(
        TrainerQuery query,
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
