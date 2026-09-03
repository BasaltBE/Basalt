using System.Text;
using System.Text.Json;

internal static class BlockIdentifierGenerator
{
    public static void Generate(string inputPath, string outputPath)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(inputPath));
        Dictionary<string, string> identifiers = new(StringComparer.Ordinal);

        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            string identifier = entry.GetProperty("identifier").GetString()
                ?? throw new InvalidDataException("A block entry has no identifier.");
            string name = ToPascalCase(identifier);

            identifiers.TryAdd(name, identifier);
        }

        StringBuilder source = new();
        source.AppendLine("using System;");
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine();
        source.AppendLine("public enum BlockIdentifier {");

        foreach (string name in identifiers.Keys.Order(StringComparer.Ordinal))
        {
            source.AppendLine($"    {name},");
        }

        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("public static class BlockIdentifierExtensions {");
        source.AppendLine("    private static readonly Dictionary<BlockIdentifier, string> ToIdentifierMap = new() {");

        foreach ((string name, string identifier) in identifiers.OrderBy(static pair => pair.Key))
        {
            source.AppendLine($"        [BlockIdentifier.{name}] = \"{identifier}\",");
        }

        source.AppendLine("    };");
        source.AppendLine();
        source.AppendLine("    private static readonly Dictionary<string, BlockIdentifier> FromIdentifierMap = new(StringComparer.Ordinal) {");

        foreach ((string name, string identifier) in identifiers.OrderBy(static pair => pair.Key))
        {
            source.AppendLine($"        [\"{identifier}\"] = BlockIdentifier.{name},");
        }

        source.AppendLine("    };");
        source.AppendLine();
        source.AppendLine("    public static string ToIdentifier(this BlockIdentifier self)");
        source.AppendLine("        => ToIdentifierMap[self];");
        source.AppendLine();
        source.AppendLine("    public static BlockIdentifier FromIdentifier(string identifier)");
        source.AppendLine("        => FromIdentifierMap.TryGetValue(identifier, out var value)");
        source.AppendLine("            ? value");
        source.AppendLine("            : throw new ArgumentException($\"Unknown block identifier: {identifier}\", nameof(identifier));");
        source.AppendLine();
        source.AppendLine("    public static bool TryFromIdentifier(string identifier, out BlockIdentifier result)");
        source.AppendLine("        => FromIdentifierMap.TryGetValue(identifier, out result);");
        source.AppendLine("}");

        string outputFile = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(outputFile)
            ?? throw new ArgumentException("The output path must include a directory.", nameof(outputPath));

        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(outputFile, source.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string ToPascalCase(string identifier)
    {
        string[] parts = identifier.Replace("minecraft:", string.Empty, StringComparison.Ordinal)
            .Split('_', StringSplitOptions.RemoveEmptyEntries);
        StringBuilder name = new();

        foreach (string part in parts)
        {
            name.Append(char.ToUpperInvariant(part[0]));
            name.Append(part.AsSpan(1));
        }

        return name.ToString();
    }
}
