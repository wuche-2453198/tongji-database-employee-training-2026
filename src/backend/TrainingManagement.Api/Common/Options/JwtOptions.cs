namespace TrainingManagement.Api.Common.Options;

/// <summary>JWT 签发与校验配置，包含签发方、受众、密钥和有效时长。</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TrainingManagement.Api";

    public string Audience { get; init; } = "TrainingManagement.Web";

    public string SigningKey { get; init; } = string.Empty;

    public int ExpireMinutes { get; init; } = 120;
}
