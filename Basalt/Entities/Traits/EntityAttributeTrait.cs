namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.BedrockProtocol.Types;

public abstract class EntityAttributeTrait : EntityTrait {
    private readonly AttributeProperties? _initialProperties;

    public abstract AttributeName Attribute { get; }

    public bool Sync { get; set; } = true;

    public float MinimumValue {
        get => GetAttribute().Minimum;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.Minimum == next) {
                return;
            }

            attribute.Minimum = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float MaximumValue {
        get => GetAttribute().Maximum;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.Maximum == next) {
                return;
            }

            attribute.Maximum = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float DefaultValue {
        get => GetAttribute().Default;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.Default == next) {
                return;
            }

            attribute.Default = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float CurrentValue {
        get => GetAttribute().Current;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.Current == next) {
                return;
            }

            attribute.Current = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    protected EntityAttributeTrait(Entity entity, AttributeProperties? properties = null) : base(entity) {
        _initialProperties = properties;
    }

    public AttributeData GetAttribute() {
        return Entity.Attributes.GetAttribute(Attribute)
            ?? throw new InvalidOperationException($"Attribute {Attribute} is not registered on entity.");
    }

    public void Reset() {
        CurrentValue = DefaultValue;
    }

    public override void OnAdd() {
        EnsureAttribute(_initialProperties ?? new AttributeProperties());
    }

    protected void EnsureAttribute(AttributeProperties properties) {
        if (Entity.Attributes.HasAttribute(Attribute)) {
            return;
        }

        float min = Truncate4(properties.MinimumValue ?? 0f);
        float max = Truncate4(properties.MaximumValue ?? 0f);
        float @default = Truncate4(properties.DefaultValue ?? 0f);
        float current = Truncate4(properties.CurrentValue ?? @default);

        Entity.Attributes.SetAttribute(new AttributeData() {
            Current = current,
            DefaultMaximum = max,
            DefaultMinimum = min,
            Default = @default,
            Maximum = max,
            Minimum = min,
            Name = Attribute.ToProtocolString(),
        });
        MarkDirty();
    }

    public override void OnRemove() {
        _ = Entity.Attributes.RemoveAttribute(Attribute);
    }

    private static float Truncate4(float value) {
        return MathF.Truncate(value * 10000f) / 10000f;
    }

    private void MarkDirty() {
        if (Sync) {
            Entity.AttributesDirty = true;
        }
    }
}






