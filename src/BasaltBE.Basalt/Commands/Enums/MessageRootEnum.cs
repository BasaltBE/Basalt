using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class MessageRootEnum() : CommandEnum(CommandParameterType.MessageRoot)
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        string message = reader.ReadRemaining();
        value = message;
        return message.Length > 0;
    }
}
