using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class EnchantmentEnum : IdentifierEnum<Enchantment> {
    public EnchantmentEnum(EnchantmentRegistry registry)
        : base("Enchantment", Index(registry.Values, static enchantment => enchantment.Identifier)) {
    }
}
