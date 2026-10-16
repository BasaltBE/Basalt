using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

/// <summary>Parses a command tail as text. It does not execute nested commands.</summary>
public sealed class CommandTailEnum() : CommandEnum(CommandParameterType.Command)
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        string command = reader.ReadRemaining();
        value = command;
        return command.Length > 0;
    }
}
