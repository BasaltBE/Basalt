using System.Globalization;

namespace BasaltBE.Basalt.Commands.Types;

public readonly record struct IntegerRange(int Minimum, int Maximum) {
    public bool Contains(int value) => value >= Minimum && value <= Maximum;

    public static bool Parse(ReadOnlySpan<char> text, out IntegerRange range) {
        range = default;
        int separator = text.IndexOf("..");
        int minimum = int.MinValue;
        int maximum = int.MaxValue;
        if (separator < 0) {
            if (!int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out minimum))
                return false;
            maximum = minimum;
        }
        else {
            ReadOnlySpan<char> first = text[..separator];
            ReadOnlySpan<char> last = text[(separator + 2)..];
            if (first.IsEmpty && last.IsEmpty ||
                !first.IsEmpty && !int.TryParse(first, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out minimum) ||
                !last.IsEmpty && !int.TryParse(last, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out maximum))
                return false;
        }
        if (minimum > maximum)
            return false;
        range = new IntegerRange(minimum, maximum);
        return true;
    }
}
