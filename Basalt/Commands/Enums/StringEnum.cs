namespace Basalt.Core.Commands;

public class StringEnum : CommandEnum
{
    public string? Value;

    public StringEnum() : base("string") { }

    public StringEnum(string? value) : base("string")
    {
        Value = value;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Value = tokens[tokenIndex];
        tokenIndex++;
        return true;
    }
}







