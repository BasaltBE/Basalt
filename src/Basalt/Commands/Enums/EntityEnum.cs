namespace Basalt.Core.Commands;

using Basalt.Core.Entities;

public sealed class EntityEnum : CommandEnum {
    const string VanillaPrefix = "minecraft:";

    public string Raw { get; private set; } = string.Empty;
    public string EntityIdentifier { get; private set; } = string.Empty;

    public EntityEnum() : base("entities") {
        EntityPalette.LoadVanilla();
        Options = [.. EntityType.Types.Keys
            .Where(static id => !string.Equals(id, "minecraft:player", StringComparison.Ordinal))
            .Select(TrimPrefix)];
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        EntityType? type = EntityType.Get(identifier);
        if (type is null)
            return false;

        EntityIdentifier = type.Identifier;
        tokenIndex++;
        return true;
    }

    static string TrimPrefix(string identifier) {
        return identifier.StartsWith(VanillaPrefix, StringComparison.Ordinal)
            ? identifier[VanillaPrefix.Length..]
            : identifier;
    }
}
