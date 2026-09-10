using System.Security.Claims;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Registration;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IRegistrationService
{
    Task<PagedResult<RegistrationResponse>> GetMyAsync(
        RegistrationQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<PagedResult<RegistrationResponse>> GetAllAsync(
        RegistrationQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationResponse?> GetByIdAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationResponse> CreateAsync(
        CreateRegistrationRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationResponse> CancelAsync(
        long regId,
        CancelRegistrationRequest? request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationResponse> MarkAbsentAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationResponse> CompleteAsync(
        long regId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<RegistrationSummaryResponse> GetSummaryAsync(
        long? courseId,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
