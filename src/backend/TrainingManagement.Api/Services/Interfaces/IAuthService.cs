using System.Security.Claims;
using TrainingManagement.Api.Dtos.Auth;

namespace TrainingManagement.Api.Services.Interfaces;

public interface IAuthService
{
    /// <summary>验证登录信息；优先查数据库，显式开启演示账号后才尝试本地账号，成功时签发令牌。</summary>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>从已认证身份提取员工编号，优先读取数据库；未找到记录时按配置尝试演示用户，最后使用令牌声明构造用户。</summary>
    Task<AuthUserResponse> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
