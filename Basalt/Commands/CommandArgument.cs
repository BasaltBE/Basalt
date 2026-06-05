namespace Basalt.Core.Commands;

public class CommandArgument
{
    public string Name;

    public CommandEnum Value;

    public CommandArgument(string name, CommandEnum value)
    {
        Name = name;
        Value = value;
    }
}







