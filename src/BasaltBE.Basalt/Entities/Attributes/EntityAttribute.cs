using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Attributes;

public readonly struct EntityAttribute {
    public readonly string Name;
    public readonly float Minimum;
    public readonly float Maximum;
    public readonly float Current;
    public readonly float DefaultMinimum;
    public readonly float DefaultMaximum;
    public readonly float Default;
    public readonly AttributeModifier[] Modifiers;

    public EntityAttribute(
        string name,
        float minimum,
        float maximum,
        float current,
        float defaultMinimum,
        float defaultMaximum,
        float @default,
        AttributeModifier[]? modifiers = null
    ) {
        Name = name;
        Minimum = minimum;
        Maximum = maximum;
        Current = current;
        DefaultMinimum = defaultMinimum;
        DefaultMaximum = defaultMaximum;
        Default = @default;
        Modifiers = modifiers ?? [];
    }

    internal AttributeData ToProtocolData() => new() {
        Name = Name,
        Minimum = Minimum,
        Maximum = Maximum,
        Current = Current,
        DefaultMinimum = DefaultMinimum,
        DefaultMaximum = DefaultMaximum,
        Default = Default,
        Modifiers = Modifiers
    };
}
