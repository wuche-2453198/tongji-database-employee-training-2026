using TrainingManagement.Api.Dtos.Auth;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateToken(AuthUserResponse user);
}
