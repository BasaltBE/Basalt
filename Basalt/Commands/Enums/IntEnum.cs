namespace Basalt.Core.Commands;

public class IntEnum : CommandEnum
{
    public int? Value;

    public IntEnum() : base("int") { }

    public IntEnum(int? value) : base("int")
    {
        Value = value;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Value = int.Parse(tokens[tokenIndex]);
        tokenIndex++;
        return true;
    }
}







