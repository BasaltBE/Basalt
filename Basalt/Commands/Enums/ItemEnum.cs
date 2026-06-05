namespace Basalt.Server.Commands;

using Basalt.Server.Item;


public class ItemEnum : CommandEnum
{
    const string VanillaPrefix = "minecraft:";

    public string Raw = string.Empty;

    public ItemType Type = ItemType.Air;

    public ItemEnum() : base("Item")
    {
        ItemPalette.LoadVanilla();
        Options = [.. ItemType.Types.Keys.Select(TrimPrefix)];
    }

    public ItemEnum(string raw, ItemType type) : base("Item")
    {
        Raw = raw;
        Type = type;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        Type = ItemType.Get(identifier) ?? throw new InvalidOperationException($"Invalid item '{Raw}' for command parameter '{parameter.Name}'.");
        tokenIndex++;
        return true;
    }

    static string TrimPrefix(string identifier)
    {
        if (!identifier.StartsWith(VanillaPrefix, StringComparison.Ordinal))
        {
            return identifier;
        }

        return identifier[VanillaPrefix.Length..];
    }
}







