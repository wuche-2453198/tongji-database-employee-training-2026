using TrainingManagement.Api.Dtos.Auth;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITokenService
{
    /// <summary>为已通过认证的用户签发访问令牌，同时返回过期时间。</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateToken(AuthUserResponse user);
}
