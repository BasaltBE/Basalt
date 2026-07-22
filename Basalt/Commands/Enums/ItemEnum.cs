namespace Basalt.Core.Commands;

using Basalt.Core.Item;

public sealed class ItemEnum : CommandEnum {
    const string VanillaPrefix = "minecraft:";

    public string Raw { get; private set; } = string.Empty;
    public ItemType Type { get; private set; } = ItemType.Air;

    public ItemEnum() : base("Item") {
        ItemPalette.LoadVanilla();
        Options = [.. ItemType.Types.Keys.Select(TrimPrefix)];
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        ItemType? type = ItemType.Get(identifier);
        if (type is null)
            return false;

        Type = type;
        tokenIndex++;
        return true;
    }

    static string TrimPrefix(string identifier) {
        return identifier.StartsWith(VanillaPrefix, StringComparison.Ordinal)
            ? identifier[VanillaPrefix.Length..]
            : identifier;
    }
}
