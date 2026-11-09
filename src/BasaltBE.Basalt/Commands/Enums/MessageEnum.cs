using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class MessageEnum() : CommandEnum(CommandParameterType.Message) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        string text = reader.ReadRemaining();
        value = text;
        return text.Length > 0;
    }
}
