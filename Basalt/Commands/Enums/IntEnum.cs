namespace Basalt.Core.Commands;

public sealed class IntEnum : CommandEnum
{
    public int? Value { get; private set; }

    public IntEnum() : base("int") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
            return false;

        if (!int.TryParse(tokens[tokenIndex], out int result))
            return false;

        Value = result;
        tokenIndex++;
        return true;
    }
}
