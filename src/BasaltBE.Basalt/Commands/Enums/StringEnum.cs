using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class StringEnum : CommandEnum
{
    public StringEnum() : base(CommandParameterType.String) { }

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        value = CommandReader.Text(token);
        return true;
    }
}
