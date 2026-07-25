using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<IReadOnlyList<TrainingCourse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<TrainingCourse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(
        long courseId,
        string status,
        CancellationToken cancellationToken);
}