using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Course;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> GetAllAsync(
        CourseQuery query,
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

    Task<(int MaxStudents, int ValidRegistrationCount, int RemainingSeats)?> GetCapacityAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<PublishCourseResponse> PublishAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task CloseAsync(
        long courseId,
        CancellationToken cancellationToken);
}
