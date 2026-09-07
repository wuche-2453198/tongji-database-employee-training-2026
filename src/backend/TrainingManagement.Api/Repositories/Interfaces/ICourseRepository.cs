using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ICourseRepository
{
    Task<bool> DepartmentExistsAsync(long deptId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<TrainingCourse> Items, long Total)> GetAllAsync(
        CourseQuery query,
        CancellationToken cancellationToken);

    Task<TrainingCourse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<long> CreateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        TrainingCourse course,
        string expectedStatus,
        CancellationToken cancellationToken);

    Task<(int MaxStudents, int ValidRegistrationCount)?> GetCapacityAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(
        long courseId,
        string expectedStatus,
        string newStatus,
        CancellationToken cancellationToken);
}
