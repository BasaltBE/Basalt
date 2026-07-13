namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;

public class CommandOverload
{
    public List<CommandParameter> Parameters = new();

    public CommandOverload Set<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(string name, bool required) where T : CommandEnum
    {
        Parameters.Add(new CommandParameter(name, typeof(T), required));
        return this;
    }
}







