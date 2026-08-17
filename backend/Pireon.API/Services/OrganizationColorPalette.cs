namespace Pireon.API.Services;

public static class OrganizationColorPalette
{
    public static readonly IReadOnlyList<string> Colors = new[]
    {
        "#0d6efd",
        "#6610f2",
        "#d63384",
        "#dc3545",
        "#fd7e14",
        "#198754",
        "#20c997",
        "#0dcaf0",
        "#6f42c1",
        "#f2711c",
        "#7b1fa2",
        "#00796b",
        "#c2185b",
        "#5d4037",
        "#546e7a"
    };

    public static string Normalize(string? color)
    {
        var value = (color ?? string.Empty).Trim();
        if (value.Length == 6 && value.All(Uri.IsHexDigit))
        {
            value = $"#{value}";
        }

        if (value.StartsWith('#') && value.Length == 7 && value[1..].All(Uri.IsHexDigit))
        {
            return value.ToLowerInvariant();
        }

        return string.Empty;
    }

    public static string ReadableText(string? color)
    {
        var value = (color ?? string.Empty).Trim();
        if (value.Length != 7 || !value.StartsWith('#'))
        {
            return "#212529";
        }

        try
        {
            var r = Convert.ToInt32(value.Substring(1, 2), 16);
            var g = Convert.ToInt32(value.Substring(3, 2), 16);
            var b = Convert.ToInt32(value.Substring(5, 2), 16);
            var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
            return luminance > 0.6 ? "#212529" : "#ffffff";
        }
        catch
        {
            return "#212529";
        }
    }

    public static string NextAvailable(IReadOnlyCollection<string>? usedColors)
    {
        var used = new HashSet<string>(usedColors ?? [], StringComparer.OrdinalIgnoreCase);
        return Colors.FirstOrDefault(color => !used.Contains(color)) ?? "#64748b";
    }
}
