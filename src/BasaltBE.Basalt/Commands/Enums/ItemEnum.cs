using BasaltBE.Basalt.Items;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class ItemEnum() : CustomEnum("Item", []) {
    public override IEnumerable<string> Options => ItemPalette.Types
        .Where(static type => type.Identifier is not ("minecraft:log" or "minecraft:log2"))
        .SelectMany(static type => type.Identifier.StartsWith("minecraft:", StringComparison.Ordinal)
            ? new[] { type.Identifier, type.Identifier[10..] }
            : [type.Identifier]);

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        string identifier = CommandReader.Text(token);
        if (!identifier.Contains(':'))
            identifier = "minecraft:" + identifier;
        if (identifier is "minecraft:log" or "minecraft:log2")
            return false;
        value = ItemPalette.Find(identifier);
        return value is not null;
    }
}
