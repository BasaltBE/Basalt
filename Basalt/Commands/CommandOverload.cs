namespace Basalt.Core.Commands;

public class CommandOverload
{
    public List<CommandParameter> Parameters = new();

    public CommandOverload Set<T>(string name, bool required) where T : CommandEnum
    {
        Parameters.Add(new CommandParameter(name, typeof(T), required));
        return this;
    }
}







