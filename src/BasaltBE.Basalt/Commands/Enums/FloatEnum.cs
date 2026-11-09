using System.Globalization;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class FloatEnum : CommandEnum {
    public readonly float Minimum;
    public readonly float Maximum;

    public FloatEnum(float minimum = -float.MaxValue, float maximum = float.MaxValue) : base(CommandParameterType.Float) {
        if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        Minimum = minimum;
        Maximum = maximum;
    }

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) ||
            !float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            return false;

        if (!float.IsFinite(number) || number < Minimum || number > Maximum) return false;
        value = number;
        return true;
    }
}
