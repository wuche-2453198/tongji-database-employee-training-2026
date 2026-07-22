namespace TrainingManagement.Api.Common.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool EnableLocalDemoUsers { get; init; } = true;

    public bool FallbackToLocalUsersOnDatabaseFailure { get; init; } = true;

    public string DatabaseDemoPassword { get; init; } = "123456";

    public string LocalDemoPassword { get; init; } = "123456";

    public IReadOnlyCollection<LocalDemoUserOptions> LocalDemoUsers { get; init; } =
        Array.Empty<LocalDemoUserOptions>();
}
