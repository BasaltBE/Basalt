using BasaltBE.Basalt.Blocks;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class BlockEnum() : CustomEnum("Block", [])
{
    public override IEnumerable<string> Options => BlockPalette.Types.SelectMany(static type =>
        type.Identifier.StartsWith("minecraft:", StringComparison.Ordinal)
            ? new[] { type.Identifier, type.Identifier[10..] }
            : [type.Identifier]);

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        string identifier = CommandReader.Text(token);
        if (!identifier.Contains(':'))
            identifier = "minecraft:" + identifier;
        value = BlockPalette.FindType(identifier);
        return value is not null;
    }
}
