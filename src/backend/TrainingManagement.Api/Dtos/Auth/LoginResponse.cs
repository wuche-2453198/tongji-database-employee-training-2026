namespace TrainingManagement.Api.Dtos.Auth;

/// <summary>登录成功结果，包含访问令牌、令牌类型、过期时间和当前用户。</summary>
public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTimeOffset ExpiresAt { get; init; }

    public AuthUserResponse User { get; init; } = new();
}
