namespace Basalt.Server.Commands;

using Basalt.Server.Entity;

public sealed class EntityEnum : CommandEnum
{
    const string VanillaPrefix = "minecraft:";

    public string Raw = string.Empty;
    public string EntityIdentifier = string.Empty;

    public EntityEnum() : base("entities")
    {
        EntityPalette.LoadVanilla();
        Options = [.. EntityType.Types.Keys
            .Where(static identifier => !string.Equals(identifier, "minecraft:player", StringComparison.Ordinal))
            .Select(TrimPrefix)];
    }

    public EntityEnum(string raw, string identifier) : base("entities")
    {
        Raw = raw;
        EntityIdentifier = identifier;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        EntityType type = EntityType.Get(identifier) ?? throw new InvalidOperationException($"Invalid entity '{Raw}' for command parameter '{parameter.Name}'.");
        EntityIdentifier = type.Identifier;
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
