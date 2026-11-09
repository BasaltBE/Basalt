using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Plugins.Packaging;

public sealed class PluginManifest {
    public static readonly JsonSerializerOptions Json = new() {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public string Id = "";
    public string Name = "";
    public string Version = "";
    public string Api = "";
    public string[] Authors = [];
    public string[] Contributors = [];

    public static PluginManifest Read(Stream stream) {
        PluginManifest manifest = JsonSerializer.Deserialize<PluginManifest>(stream, Json)
            ?? throw new InvalidDataException("Plugin manifest is empty.");

        manifest.Validate();
        return manifest;
    }

    public void Validate() {
        if (string.IsNullOrWhiteSpace(Id) || Id.Length > 64 ||
            !Id.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            throw new InvalidDataException("Plugin id must contain 1 to 64 letters, digits, hyphens or underscores.");

        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 128 ||
            Name.EndsWith('.') || Name.EndsWith(' ') ||
            Name.Any(character => character < 32 || "<>:\"/\\|?*".Contains(character)))
            throw new InvalidDataException("Plugin name must be a valid file name.");

        string stem = Name.Split('.')[0].ToUpperInvariant();
        bool numberedDevice = stem.Length == 4 && stem[3] is >= '1' and <= '9' &&
            (stem.StartsWith("COM") || stem.StartsWith("LPT"));
        if (stem is "CON" or "PRN" or "AUX" or "NUL" || numberedDevice)
            throw new InvalidDataException("Plugin name is reserved by the operating system.");

        if (!System.Version.TryParse(Version, out _))
            throw new InvalidDataException("Plugin version must be a numeric release version, such as 1.0.0.");

        if (Authors is null || Authors.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Plugin authors must be a list of non-empty names.");
        if (Contributors is null || Contributors.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Plugin contributors must be a list of non-empty names.");

        _ = ApiMajor(Api);
    }

    public static int ApiMajor(string value) {
        if (string.IsNullOrEmpty(value)) throw new InvalidDataException("Plugin api is required.");

        string[] parts = value.Split('.');
        if (parts.Length is not (1 or 3))
            throw new InvalidDataException("Plugin api must be a major version or a three-part version.");

        int major = 0;
        for (int index = 0; index < parts.Length; index++) {
            string part = parts[index];
            if (!part.All(char.IsAsciiDigit) ||
                !int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int component))
                throw new InvalidDataException("Plugin api must be a major version or a three-part version.");

            if (index == 0) major = component;
        }

        return major;
    }

    public string? ApiMismatch(int expected) {
        int got = ApiMajor(Api);
        if (got == expected) return null;

        return FormattableString.Invariant($"Could not load {Name}: API version mismatch (got: {got}, expected: {expected}).");
    }
}
