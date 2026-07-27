namespace TrainingManagement.Api.Common.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool EnableLocalDemoUsers { get; init; }

    public bool FallbackToLocalUsersOnDatabaseFailure { get; init; }

    public string LocalDemoPassword { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalDemoUserOptions> LocalDemoUsers { get; init; } =
        Array.Empty<LocalDemoUserOptions>();
}
