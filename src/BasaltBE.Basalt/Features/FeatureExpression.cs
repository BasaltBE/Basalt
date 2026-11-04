using System;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Features;

public readonly struct FeatureExpression {
    enum Kind : byte {
        Invalid,
        Heightmap,
        AnyTag
    }

    readonly Kind kind;
    readonly string? tag;

    FeatureExpression(Kind kind, string? tag) {
        this.kind = kind;
        this.tag = tag;
    }

    public bool Valid => kind is Kind.Heightmap or Kind.AnyTag;

    public static FeatureExpression Parse(string source) {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ReadOnlySpan<char> expression = source.AsSpan().Trim();
        int open = expression.IndexOf('(');
        if (open < 0 || expression[^1] != ')')
            throw new FormatException($"Unsupported feature expression '{source}'.");

        ReadOnlySpan<char> name = expression[..open].Trim();
        ReadOnlySpan<char> arguments = expression[(open + 1)..^1].Trim();
        if (name.SequenceEqual("query.heightmap")) {
            int separator = arguments.IndexOf(',');
            if (separator >= 0 &&
                arguments[..separator].Trim().SequenceEqual("variable.worldx") &&
                arguments[(separator + 1)..].Trim().SequenceEqual("variable.worldz"))
                return new FeatureExpression(Kind.Heightmap, null);
        }

        if (name.SequenceEqual("query.any_tag") && arguments.Length >= 3 &&
            arguments[0] is '\'' or '"' && arguments[^1] == arguments[0]) {
            string tag = arguments[1..^1].ToString();
            if (string.IsNullOrWhiteSpace(tag) || tag.Contains(arguments[0]))
                throw new FormatException($"Feature expression '{source}' has an invalid tag.");
            return new FeatureExpression(Kind.AnyTag, tag);
        }

        throw new FormatException($"Unsupported feature expression '{source}'.");
    }

    public int EvaluateHeightmap(Chunk chunk, int minimumY, int maximumY, int worldX, int worldZ) {
        if (kind != Kind.Heightmap)
            throw new InvalidOperationException("This feature expression does not return a heightmap value.");
        ArgumentNullException.ThrowIfNull(chunk);
        if (worldX >> 4 != chunk.X || worldZ >> 4 != chunk.Z)
            throw new ArgumentOutOfRangeException(nameof(worldX), "Heightmap coordinates must belong to the supplied chunk.");
        if (minimumY > maximumY)
            throw new ArgumentOutOfRangeException(nameof(minimumY));

        int localX = worldX & 15;
        int localZ = worldZ & 15;
        for (int y = maximumY; y >= minimumY; y--) {
            if (chunk.GetPermutation(localX, y, localZ).Identifier is not ("minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
                return y + 1;
        }

        return minimumY;
    }

    public bool MatchesTags(ReadOnlySpan<string> tags) {
        if (kind != Kind.AnyTag)
            throw new InvalidOperationException("This feature expression does not match block tags.");
        foreach (string candidate in tags) {
            if (string.Equals(candidate, tag, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
