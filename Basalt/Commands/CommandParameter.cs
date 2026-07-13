namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;

public class CommandParameter
{
    public string Name;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type Enum;

    public bool Required;

    public CommandParameter(string name, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type @enum, bool required)
    {
        Name = name;
        Enum = @enum;
        Required = required;
    }
}







