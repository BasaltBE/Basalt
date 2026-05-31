namespace Basalt.Server.Entity.Traits;

using Basalt.Server.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;


public abstract class EntityAttributeTrait : EntityTrait
{
    private readonly AttributeProperties? _initialProperties;

    public abstract AttributeName Attribute { get; }

    public bool Sync { get; set; } = true;

    public float MinimumValue
    {
        get => GetAttribute().Min;
        set
        {
            ProtoAttribute attribute = GetAttribute();
            attribute.Min = Truncate4(value);
            Entity.Attributes.SetAttribute(attribute);
        }
    }

    public float MaximumValue
    {
        get => GetAttribute().Max;
        set
        {
            ProtoAttribute attribute = GetAttribute();
            attribute.Max = Truncate4(value);
            Entity.Attributes.SetAttribute(attribute);
        }
    }

    public float DefaultValue
    {
        get => GetAttribute().Default;
        set
        {
            ProtoAttribute attribute = GetAttribute();
            attribute.Default = Truncate4(value);
            Entity.Attributes.SetAttribute(attribute);
        }
    }

    public float CurrentValue
    {
        get => GetAttribute().Current;
        set
        {
            ProtoAttribute attribute = GetAttribute();
            attribute.Current = Truncate4(value);
            Entity.Attributes.SetAttribute(attribute);
        }
    }

    protected EntityAttributeTrait(Entity entity, AttributeProperties? properties = null) : base(entity)
    {
        _initialProperties = properties;
    }

    public ProtoAttribute GetAttribute()
    {
        return Entity.Attributes.GetAttribute(Attribute)
            ?? throw new InvalidOperationException($"Attribute {Attribute} is not registered on entity.");
    }

    public void Reset()
    {
        CurrentValue = DefaultValue;
    }

    public override void OnAdd()
    {
        EnsureAttribute(_initialProperties ?? new AttributeProperties());
    }

    protected void EnsureAttribute(AttributeProperties properties)
    {
        if (Entity.Attributes.HasAttribute(Attribute))
        {
            return;
        }

        float min = Truncate4(properties.MinimumValue ?? 0f);
        float max = Truncate4(properties.MaximumValue ?? 0f);
        float @default = Truncate4(properties.DefaultValue ?? 0f);
        float current = Truncate4(properties.CurrentValue ?? @default);

        Entity.Attributes.SetAttribute(new ProtoAttribute(min, max, current, @default, Attribute));
    }

    public override void OnRemove()
    {
        _ = Entity.Attributes.RemoveAttribute(Attribute);
    }

    private static float Truncate4(float value)
    {
        return MathF.Truncate(value * 10000f) / 10000f;
    }
}






