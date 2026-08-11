namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using BedrockProtocol.Types;

public abstract class EntityAttributeTrait : EntityTrait {
    private readonly AttributeProperties? _initialProperties;

    public abstract AttributeName Attribute { get; }

    public bool Sync { get; set; } = true;

    public float MinimumValue {
        get => GetAttribute().MinValue;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.MinValue == next) {
                return;
            }

            attribute.MinValue = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float MaximumValue {
        get => GetAttribute().MaxValue;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.MaxValue == next) {
                return;
            }

            attribute.MaxValue = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float DefaultValue {
        get => GetAttribute().DefaultValue;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.DefaultValue == next) {
                return;
            }

            attribute.DefaultValue = next;
            Entity.Attributes.SetAttribute(attribute);
            MarkDirty();
        }
    }

    public float CurrentValue {
        get => GetAttribute().CurrentValue;
        set {
            AttributeData attribute = GetAttribute();
            float next = Truncate4(value);
            if (attribute.CurrentValue == next) {
                return;
            }

            attribute.CurrentValue = next;
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
            CurrentValue = current,
            DefaultMaxValue = max,
            DefaultMinValue = min,
            DefaultValue = @default,
            MaxValue = max,
            MinValue = min,
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






