using System.Globalization;
using System.Reflection;
using System.Text;

namespace BasaltBE.Basalt.Properties;

public static class ServerPropertiesFile {
    private static readonly ServerPropertyDefinition[] definitions = typeof(ServerProperties)
        .GetFields(BindingFlags.Instance | BindingFlags.Public)
        .Select(static field => new ServerPropertyDefinition(
            field,
            field.GetCustomAttribute<ServerPropertyAttribute>()
                ?? throw new InvalidOperationException($"{field.Name} has no server property metadata.")
        ))
        .OrderBy(static definition => definition.Metadata.Order)
        .ToArray();
    private static readonly Dictionary<string, ServerPropertyDefinition> definitionsByKey =
        definitions.ToDictionary(static definition => definition.Metadata.Key, StringComparer.Ordinal);

    public static ServerProperties LoadFromPath(string path) {
        ServerProperties properties = new();
        if (!File.Exists(path)) return properties;

        object boxedProperties = properties;
        foreach (string line in File.ReadLines(path)) {
            ReadOnlySpan<char> entry = line.AsSpan().Trim();
            if (entry.IsEmpty || entry[0] is '#' or '!') continue;

            int separator = entry.IndexOf('=');
            if (separator <= 0) continue;

            string key = entry[..separator].Trim().ToString();
            ReadOnlySpan<char> value = entry[(separator + 1)..].Trim();
            if (definitionsByKey.TryGetValue(key, out ServerPropertyDefinition definition) &&
                Parse(definition.Field.FieldType, value, out object? parsed))
                definition.Field.SetValue(boxedProperties, parsed);
        }

        properties = (ServerProperties)boxedProperties;
        properties.MaxPlayers = Math.Max(properties.MaxPlayers, 1);
        properties.MaxViewDistance = Math.Clamp(properties.MaxViewDistance, 0, 96);
        properties.AutosaveIntervalSeconds = Math.Clamp(properties.AutosaveIntervalSeconds, 0, 86400);
        return properties;
    }

    public static void SaveToPath(string path, ServerProperties properties) {
        StringBuilder text = new();
        string? category = null;
        foreach (ServerPropertyDefinition definition in definitions) {
            ServerPropertyAttribute metadata = definition.Metadata;
            if (!string.Equals(category, metadata.Category, StringComparison.Ordinal)) {
                if (text.Length > 0) text.AppendLine();

                text.Append("# ").AppendLine(metadata.Category);
                category = metadata.Category;
            }

            text.Append("# ").AppendLine(metadata.Comment);
            text.Append(metadata.Key).Append('=')
                .Append(Convert.ToString(definition.Field.GetValue(properties), CultureInfo.InvariantCulture))
                .AppendLine();
        }

        File.WriteAllText(path, text.ToString());
    }

    private static bool Parse(Type type, ReadOnlySpan<char> value, out object? parsed) {
        if (type == typeof(string)) {
            parsed = value.ToString();
            return true;
        }

        if (type == typeof(bool) && bool.TryParse(value, out bool boolean)) {
            parsed = boolean;
            return true;
        }

        if (type == typeof(ushort) &&
            ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort unsignedShort)) {
            parsed = unsignedShort;
            return true;
        }

        if (type == typeof(int) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer)) {
            parsed = integer;
            return true;
        }

        parsed = null;
        return false;
    }

    private readonly record struct ServerPropertyDefinition(FieldInfo Field, ServerPropertyAttribute Metadata);
}
