using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ITrainerRepository
{
    Task<(IReadOnlyList<Trainer> Items, long Total)> GetAllAsync(
        TrainerQuery query,
        CancellationToken cancellationToken);

    Task<Trainer?> GetByIdAsync(
        long trainerId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        long trainerId,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        Trainer trainer,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        Trainer trainer,
        CancellationToken cancellationToken);
}
