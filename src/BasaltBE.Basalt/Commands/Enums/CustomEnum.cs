using System.Collections.Frozen;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public class CustomEnum : CommandEnum {
    private readonly FrozenSet<string> options;
    public readonly string Identifier;
    public readonly bool Soft;
    public virtual IEnumerable<string> Options => options;

    public CustomEnum(string identifier, IEnumerable<string> values, bool soft = false)
        : base(CommandParameterType.String) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        Identifier = identifier;
        Soft = soft;
        options = values.ToFrozenSet(StringComparer.Ordinal);
    }

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        string text = CommandReader.Text(token);
        if (!Soft && !options.Contains(text))
            return false;
        value = text;
        return true;
    }
}
