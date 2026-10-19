namespace BasaltBE.Basalt.Blocks.Traits.Types;

[AttributeUsage(AttributeTargets.Class)]
public sealed class BlockTraitAppliesToAttribute(params string[] identifiers) : Attribute {
    public readonly string[] Identifiers = identifiers;
}
