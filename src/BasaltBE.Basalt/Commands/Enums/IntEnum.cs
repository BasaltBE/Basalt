using System.Globalization;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class IntEnum : CommandEnum {
    public readonly int Minimum;
    public readonly int Maximum;

    public IntEnum(int minimum = int.MinValue, int maximum = int.MaxValue) : base(CommandParameterType.Int) {
        if (minimum > maximum)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        Minimum = minimum;
        Maximum = maximum;
    }

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) ||
            !int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int number) ||
            number < Minimum || number > Maximum)
            return false;
        value = number;
        return true;
    }
}
