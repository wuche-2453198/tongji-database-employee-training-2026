using System.Security.Claims;
using TrainingManagement.Api.Dtos.Attendance;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IAttendanceService
{
    Task<AttendanceResponse> SignInAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<AttendanceResponse> ManualSignInAsync(
        CreateManualAttendanceRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
