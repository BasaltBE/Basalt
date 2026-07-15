namespace Basalt.Core.Commands;

/// <summary>
/// Base class for command parameter types. Each subclass knows how to parse its tokens
/// and exposes an identifier and options for the AvailableCommandsPacket.
/// </summary>
public abstract class CommandEnum
{
    /// <summary>
    /// Protocol identifier for this enum type.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Enum options shown in autocomplete. Empty for hardcoded types (int, string, position, etc.)
    /// </summary>
    public virtual string[] Options { get; protected set; } = [];

    protected CommandEnum(string identifier)
    {
        Identifier = identifier;
    }

    /// <summary>
    /// Parse tokens starting at tokenIndex. Advance tokenIndex past consumed tokens.
    /// Returns true on success.
    /// </summary>
    public abstract bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex);
}
