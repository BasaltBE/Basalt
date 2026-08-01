namespace Basalt.Core.Commands;

using Basalt.Core.Blocks;

public sealed class BlockEnum : CommandEnum {
    const string VanillaPrefix = "minecraft:";

    public string Raw { get; private set; } = string.Empty;
    public BlockType Type { get; private set; } = null!;

    public BlockEnum() : base("Block") {
        Options = [.. BlockType.Types.Keys.Select(TrimPrefix)];
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        BlockType? type = BlockType.Get(identifier);
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
