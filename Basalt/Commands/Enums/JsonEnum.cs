namespace Basalt.Server.Commands;

public class JsonEnum : CommandEnum
{
    public string? Value;

    public JsonEnum() : base("json") { }

    public JsonEnum(string? value) : base("json")
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







