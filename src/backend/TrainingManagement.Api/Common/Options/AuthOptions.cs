namespace TrainingManagement.Api.Common.Options;

/// <summary>认证演示与异常回退配置；真实数据库验证应关闭演示账号和回退开关。</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>显式启用本地演示账号；默认关闭，数据库未找到用户时也受此开关控制。</summary>
    public bool EnableLocalDemoUsers { get; init; }

    /// <summary>数据库异常时允许演示回退；必须同时启用本地演示账号才生效。</summary>
    public bool FallbackToLocalUsersOnDatabaseFailure { get; init; }

    public string LocalDemoPassword { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalDemoUserOptions> LocalDemoUsers { get; init; } =
        Array.Empty<LocalDemoUserOptions>();
}
