namespace BasaltBE.Basalt.Commands.Enums;

public sealed class BoolEnum() : CustomEnum("Boolean", ["true", "false"])
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        if (token.SequenceEqual("true"))
            value = true;
        else if (token.SequenceEqual("false"))
            value = false;
        return value is not null;
    }
}
