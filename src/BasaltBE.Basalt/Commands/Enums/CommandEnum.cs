using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

/// <summary>Immutable parser shared by command executions. Store parsed values in the output.</summary>
public abstract class CommandEnum(CommandParameterType type) {
    public readonly CommandParameterType Type = type;
    public abstract bool Parse(CommandContext context, ref CommandReader reader, out object? value);
}
