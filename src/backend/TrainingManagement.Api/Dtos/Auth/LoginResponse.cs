namespace TrainingManagement.Api.Dtos.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string TokenType { get; init; } = "Bearer";

    public DateTimeOffset ExpiresAt { get; init; }

    public AuthUserResponse User { get; init; } = new();
}
