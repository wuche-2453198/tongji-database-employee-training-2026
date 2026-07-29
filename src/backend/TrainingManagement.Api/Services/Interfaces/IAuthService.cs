using System.Security.Claims;
using TrainingManagement.Api.Dtos.Auth;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthUserResponse> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
