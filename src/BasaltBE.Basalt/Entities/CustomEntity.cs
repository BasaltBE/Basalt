using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Types.Components;
using BasaltBE.Basalt.Items;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Entities;

public sealed class CustomEntity {
    public readonly EntityType Type;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    internal CustomEntity(EntityType type) => Type = type;

    public CustomEntity SetHealth(float maximum) {
        if (!float.IsFinite(maximum) || maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
        Mutable();

        Type.PropertiesPayload ??= new();
        Type.PropertiesPayload.Components.Health = new() { Max = maximum, Value = new EntityNumericValue { Value = maximum } };
        return SetComponent("minecraft:health");
    }

    public CustomEntity SetMovementSpeed(float speed) {
        if (!float.IsFinite(speed) || speed < 0) throw new ArgumentOutOfRangeException(nameof(speed));
        Mutable();

        Type.PropertiesPayload ??= new();
        Type.PropertiesPayload.Components.Movement = new() { Value = new EntityNumericValue { Value = speed } };
        return SetComponent("minecraft:movement");
    }

    public CustomEntity SetCollisionBox(float width, float height) {
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0 || width > 64 || height > 64)
            throw new ArgumentOutOfRangeException(nameof(width));
        Mutable();

        Type.PropertiesPayload ??= new();
        Type.PropertiesPayload.Components.CollisionBox = new() { Width = width, Height = height };
        return SetComponent("minecraft:collision_box");
    }

    public CustomEntity SetFamilies(params string[] families) {
        ArgumentNullException.ThrowIfNull(families);
        if (families.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Use non-empty family names.", nameof(families));
        Mutable();
        Type.Families = families.Distinct(StringComparer.Ordinal).ToArray();
        return this;
    }

    public CustomEntity SetSummonable(bool value = true) {
        Mutable();
        Type.Summonable = value;
        return this;
    }

    public CustomEntity AddTrait<TTrait>() where TTrait : EntityTrait {
        Mutable();
        EntityPalette.RegisterTrait<TTrait>(Type.Identifier);
        return this;
    }

    public CustomEntity SetComponent(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        Mutable();
        if (!Type.Components.Contains(identifier, StringComparer.Ordinal)) Type.Components = [.. Type.Components, identifier];
        EntityPalette.Invalidate(Type);
        return this;
    }

    public CustomEntity SetSpawnEgg(string identifier, string texture, string displayName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(texture);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Mutable();
        CustomItem item = ItemPalette.Create(identifier)
            .SetDisplayName(displayName)
            .SetIcon(texture)
            .SetCreativeCategory(CreativeItemCategory.Nature);
        item.Type.PropertiesPayload.EntityPlacer = new(StringComparer.Ordinal) { ["entity"] = Type.Identifier };
        item.SetComponent("minecraft:entity_placer", new CompoundTag()
            .Add("entity", new StringTag { Value = Type.Identifier })
            .Add("use_on", new ListTag { ElementType = Nbt.Enums.TagType.String })
            .Add("dispense_on", new ListTag { ElementType = Nbt.Enums.TagType.String }));
        Type.HasSpawnEgg = true;
        return this;
    }

    private void Mutable() {
        if (EntityPalette.Frozen || Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Configure entities on their creating thread before starting the server.");
        EntityPalette.Invalidate(Type);
    }
}
