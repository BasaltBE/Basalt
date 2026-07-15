namespace Basalt.Core.Commands;

/// <summary>
/// Base class for custom enums with a fixed set of string values (e.g. GameMode, etc.)
/// Subclass and define a static Values field/property.
/// </summary>
public abstract class CustomEnum : CommandEnum
{
    public string? Value { get; private set; }

    protected CustomEnum(string identifier, string[] values) : base(identifier)
    {
        Options = values;
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
            return false;

        string token = tokens[tokenIndex];
        for (int i = 0; i < Options.Length; i++)
        {
            if (string.Equals(Options[i], token, StringComparison.OrdinalIgnoreCase))
            {
                Value = Options[i];
                tokenIndex++;
                return true;
            }
        }
        return false;
    }
}
