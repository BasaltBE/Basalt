using System.Collections.Frozen;
using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class EnchantmentEnum() : IdentifierEnum<EnchantmentOption>("Enchantment", Values)
{
    // TODO: Add enchantment options when the data is available.
    private static readonly FrozenDictionary<string, EnchantmentOption> Values = FrozenDictionary<string, EnchantmentOption>.Empty;
}
