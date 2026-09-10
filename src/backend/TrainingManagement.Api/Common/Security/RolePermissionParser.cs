using System.Text.Json;

namespace TrainingManagement.Api.Common.Security;

public static class RolePermissionParser
{
    /// <summary>解析 JSON 或分隔符格式的权限列表，去重排序；没有有效权限时使用角色默认值。</summary>
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

    /// <summary>尝试读取权限数组或包含 permissions 数组的对象；非 JSON 内容留给分隔符解析。</summary>
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
            // 非 JSON 权限列表不视为错误，调用方继续按分隔符解析。
        }
    }

    /// <summary>收集 JSON 数组中的非空字符串权限，忽略其他类型元素。</summary>
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
