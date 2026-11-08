using System.Globalization;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class ValueEnum() : CommandEnum(CommandParameterType.Value) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token)) return false;

        bool relative = token[0] == '~';
        if (relative) token = token[1..];
        float number = 0;
        if ((!relative || !token.IsEmpty) &&
            (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || !float.IsFinite(number)))
            return false;
        value = new CommandCoordinate(number, relative);
        return true;
    }
}
