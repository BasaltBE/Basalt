namespace Basalt.Core.Commands;

using Basalt.Core.Item.Enchantment;

/// <summary>
/// Command parameter that autocompletes enchantment identifiers.
/// </summary>
public sealed class EnchantmentEnum : CommandEnum {
    public string Raw { get; private set; } = string.Empty;
    public EnchantmentType? Type { get; private set; }

    public EnchantmentEnum() : base("enchantment") {
        Options = [.. EnchantmentType.All.Values.Select(e => e.Identifier)];
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        Raw = tokens[tokenIndex];
        Type = EnchantmentType.Get(Raw);
        if (Type is null) {
            if (int.TryParse(Raw, out int id)) {
                Type = EnchantmentType.Get(id);
            }
        }

        tokenIndex++;
        return Type is not null;
    }
}
