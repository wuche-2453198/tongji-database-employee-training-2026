namespace TrainingManagement.Api.Common.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TrainingManagement.Api";

    public string Audience { get; init; } = "TrainingManagement.Web";

    public string SigningKey { get; init; } = string.Empty;

    public int ExpireMinutes { get; init; } = 120;
}
