namespace BasaltBE.Basalt.Commands.Types;

public sealed class CommandOverload(params CommandParameter[] parameters)
{
    public readonly CommandParameter[] Parameters = parameters;
}
