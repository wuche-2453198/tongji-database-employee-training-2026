using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IAttendanceRepository
{
    Task<SignInResult> SignInAsync(
        AttendanceRecord attendance,
        decimal actualHours,
        CancellationToken cancellationToken);
}
