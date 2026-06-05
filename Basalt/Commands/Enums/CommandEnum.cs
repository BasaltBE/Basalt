namespace Basalt.Core.Commands;

public abstract class CommandEnum
{
    public string Identifier;

    public string[] Options;

    protected CommandEnum(string identifier, params string[] options)
    {
        Identifier = identifier;
        Options = options;
    }

    public abstract bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex);
}







