namespace Basalt.Core.Commands;

public sealed class JsonEnum : CommandEnum
{
    public string? Value { get; private set; }

    public JsonEnum() : base("json") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
            return false;

        Value = tokens[tokenIndex];
        tokenIndex++;
        return true;
    }
}
