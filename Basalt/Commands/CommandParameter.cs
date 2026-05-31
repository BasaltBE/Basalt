namespace Basalt.Server.Commands;

public class CommandParameter
{
    public string Name;

    public Type Enum;

    public bool Required;

    public CommandParameter(string name, Type @enum, bool required)
    {
        Name = name;
        Enum = @enum;
        Required = required;
    }
}







