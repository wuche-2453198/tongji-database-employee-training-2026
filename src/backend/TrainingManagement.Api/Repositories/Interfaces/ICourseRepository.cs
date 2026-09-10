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

    /// <summary>在调用方提供的事务会话内执行状态流转，供课程发布与预算占用同事务提交。</summary>
    Task<bool> UpdateStatusAsync(
        long courseId,
        string expectedStatus,
        string newStatus,
        IDbSession session,
        CancellationToken cancellationToken);
}
