namespace Basalt.Core.Commands;

public sealed class StringEnum : CommandEnum
{
    public string? Value { get; private set; }

    public StringEnum() : base("string") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
            return false;

        Value = tokens[tokenIndex];
        tokenIndex++;
        return true;
    }
}
