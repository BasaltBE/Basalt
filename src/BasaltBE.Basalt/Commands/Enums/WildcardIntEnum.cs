using System.Globalization;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class WildcardIntEnum() : CommandEnum(CommandParameterType.WildcardInt) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token)) return false;

        if (token.SequenceEqual("*")) value = new WildcardInteger(null);
        else if (int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number))
            value = new WildcardInteger(number);
        return value is not null;
    }
}
