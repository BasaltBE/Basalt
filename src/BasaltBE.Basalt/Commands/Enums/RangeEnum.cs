using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class RangeEnum() : CommandEnum(CommandParameterType.FullIntegerRange) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) || !IntegerRange.Parse(token, out IntegerRange range)) return false;

        value = range;
        return true;
    }
}
