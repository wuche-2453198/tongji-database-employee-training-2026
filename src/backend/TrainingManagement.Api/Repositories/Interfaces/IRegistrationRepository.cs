using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IRegistrationRepository
{
    Task<(IReadOnlyList<RegistrationRecord> Items, long Total)> GetAllAsync(
        RegistrationQuery query,
        long? empId,
        CancellationToken cancellationToken);

    Task<RegistrationRecord?> GetByIdAsync(
        long regId,
        CancellationToken cancellationToken);

    Task<RegistrationContextRecord?> GetContextAsync(
        long regId,
        CancellationToken cancellationToken);

    Task<long?> FindFiledRequestIdAsync(
        long empId,
        long courseId,
        CancellationToken cancellationToken);

    Task<EmployeeEligibilityRecord?> GetEmployeeEligibilityAsync(
        long empId,
        CancellationToken cancellationToken);

    Task<CourseEligibilityRecord?> GetCourseEligibilityAsync(
        long courseId,
        CancellationToken cancellationToken);

    Task<RegistrationCreateResult> CreateAsync(
        RegistrationRecord registration,
        CancellationToken cancellationToken);

    Task<bool> CancelAsync(
        long regId,
        string? cancelReason,
        CancellationToken cancellationToken);

    Task<bool> MarkAbsentAsync(
        long regId,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        long regId,
        CancellationToken cancellationToken);

    Task<RegistrationSummaryRecord> GetSummaryAsync(
        long? courseId,
        CancellationToken cancellationToken);
}
