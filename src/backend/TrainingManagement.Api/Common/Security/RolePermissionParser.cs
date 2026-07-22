using System.Text.Json;

namespace TrainingManagement.Api.Common.Security;

public static class RolePermissionParser
{
    public static IReadOnlyCollection<string> Parse(string? rawPermissions, string roleCode)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(rawPermissions))
        {
            TryParseJson(rawPermissions, permissions);

            if (permissions.Count == 0)
            {
                foreach (var item in rawPermissions.Split(
                    new[] { ',', ';', '\r', '\n', '\t', ' ' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    permissions.Add(item);
                }
            }
        }

        if (permissions.Count == 0)
        {
            foreach (var permission in PermissionCodes.GetDefaultPermissions(roleCode))
            {
                permissions.Add(permission);
            }
        }

        return permissions.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void TryParseJson(string rawPermissions, HashSet<string> permissions)
    {
        try
        {
            using var document = JsonDocument.Parse(rawPermissions);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                AddArrayPermissions(root, permissions);
                return;
            }

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("permissions", out var property)
                && property.ValueKind == JsonValueKind.Array)
            {
                AddArrayPermissions(property, permissions);
            }
        }
        catch (JsonException)
        {
            // Non-JSON permission lists are accepted and parsed by delimiters.
        }
    }

    private static void AddArrayPermissions(JsonElement array, HashSet<string> permissions)
    {
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    permissions.Add(value);
                }
            }
        }
    }
}
