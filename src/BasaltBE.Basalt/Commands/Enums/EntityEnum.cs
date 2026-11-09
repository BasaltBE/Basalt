using BasaltBE.Basalt.Entities.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class EntityEnum() : CustomEnum("EntityType", []) {
    public override IEnumerable<string> Options => EntityPalette.Types.SelectMany(static type =>
        type.Identifier.StartsWith("minecraft:", StringComparison.Ordinal)
            ? new[] { type.Identifier, type.Identifier[10..] }
            : [type.Identifier]);

    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token)) return false;

        string identifier = CommandReader.Text(token);
        if (!identifier.Contains(':')) identifier = "minecraft:" + identifier;

        value = EntityPalette.Find(identifier);
        return value is not null;
    }
}
