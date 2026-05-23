namespace Basalt.Commands;

public abstract class CommandEnum
{
    public string Identifier;

    public string[] Options;

    protected CommandEnum(string identifier, params string[] options)
    {
        Identifier = identifier;
        Options = options;
    }
}
