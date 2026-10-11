using System.Globalization;
using System.Reflection;
using System.Text;

namespace BasaltBE.Basalt.Properties;

public static class ServerPropertiesFile {
    private static readonly FieldInfo[] fields = typeof(ServerProperties)
        .GetFields(BindingFlags.Instance | BindingFlags.Public)
        .OrderBy(static field => field.GetCustomAttribute<ServerPropertyAttribute>()?.Order ?? int.MaxValue)
        .ToArray();

    public static ServerProperties LoadFromPath(string path) {
        ServerProperties properties = new();
        if (!File.Exists(path))
            return properties;

        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string line in File.ReadLines(path)) {
            ReadOnlySpan<char> entry = line.AsSpan().Trim();
            if (entry.IsEmpty || entry[0] is '#' or '!')
                continue;

            int separator = entry.IndexOf('=');
            if (separator <= 0)
                continue;

            string key = entry[..separator].Trim().ToString();
            string value = entry[(separator + 1)..].Trim().ToString();
            values[key] = value;
        }

        object boxed = properties;
        foreach (FieldInfo field in fields) {
            ServerPropertyAttribute metadata = field.GetCustomAttribute<ServerPropertyAttribute>()!;
            if (values.TryGetValue(metadata.Key, out string? value) && Parse(field.FieldType, value, out object? parsed))
                field.SetValue(boxed, parsed);
        }

        properties = (ServerProperties)boxed;
        properties.MaxPlayers = Math.Max(properties.MaxPlayers, 1);
        properties.MaxViewDistance = Math.Clamp(properties.MaxViewDistance, 0, 96);
        return properties;
    }

    public static void SaveToPath(string path, ServerProperties properties) {
        StringBuilder text = new();
        string? category = null;
        foreach (FieldInfo field in fields) {
            ServerPropertyAttribute metadata = field.GetCustomAttribute<ServerPropertyAttribute>()!;
            if (!string.Equals(category, metadata.Category, StringComparison.Ordinal)) {
                if (text.Length > 0)
                    text.AppendLine();
                text.Append("# ").AppendLine(metadata.Category);
                category = metadata.Category;
            }

            text.Append("# ").AppendLine(metadata.Comment);
            text.Append(metadata.Key).Append('=')
                .Append(Convert.ToString(field.GetValue(properties), CultureInfo.InvariantCulture))
                .AppendLine();
        }

        File.WriteAllText(path, text.ToString());
    }

    private static bool Parse(Type type, string value, out object? parsed) {
        if (type == typeof(string)) {
            parsed = value;
            return true;
        }

        if (type == typeof(bool) && bool.TryParse(value, out bool boolean)) {
            parsed = boolean;
            return true;
        }

        if (type == typeof(ushort) && ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort unsignedShort)) {
            parsed = unsignedShort;
            return true;
        }

        if (type == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer)) {
            parsed = integer;
            return true;
        }

        parsed = null;
        return false;
    }
}
