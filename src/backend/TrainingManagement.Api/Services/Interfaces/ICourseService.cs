using TrainingManagement.Api.Dtos.Course;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ICourseService
{
    Task<IReadOnlyCollection<CourseResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<CourseResponse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<CourseResponse> CreateAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken);

    Task<CourseResponse> UpdateAsync(
        long courseId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken);

    Task PublishAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task CloseAsync(
        long courseId,
        CancellationToken cancellationToken);
}