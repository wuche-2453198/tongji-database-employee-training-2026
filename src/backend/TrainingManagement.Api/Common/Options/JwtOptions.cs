namespace TrainingManagement.Api.Common.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TrainingManagement.Api";

    public string Audience { get; init; } = "TrainingManagement.Web";

    public string SigningKey { get; init; } = "dev-only-training-management-signing-key-change-me";

    public int ExpireMinutes { get; init; } = 120;
}
