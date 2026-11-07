using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Nbt.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;

namespace BasaltBE.Basalt.Entities;

public sealed class EntityRegistry
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    public CustomEntity Create(string identifier)
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Configure entities on the server startup thread.");
        return EntityPalette.Create(identifier);
    }

    public EntityType? Find(string identifier) => EntityPalette.Find(identifier);

    public EntityType Get(string identifier) => Find(identifier)
        ?? throw new KeyNotFoundException($"Entity '{identifier}' was not found.");

    public CompoundTag Save(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player || entity.Type is null ||
            entity.GetType() != typeof(Entity) && entity is not ItemEntity and not FallingBlockEntity)
            throw new ArgumentException("Save a registered non-player entity. Use traits for persistent custom behavior.", nameof(entity));
        entity.SpawnedDimension?.World?.CheckOwnership();

        if (!float.IsFinite(entity.Position.X) || !float.IsFinite(entity.Position.Y) || !float.IsFinite(entity.Position.Z) ||
            !float.IsFinite(entity.Rotation.X) || !float.IsFinite(entity.Rotation.Y))
            throw new InvalidDataException("Entity position or rotation is invalid.");

        CompoundTag tag = new();
        if (entity.StorageTag is { } stored)
            foreach ((string name, Tag value) in stored.Tags)
                tag.Add(name, value);

        tag.Add("SimulationAge", new LongTag { Value = checked((long)entity.SimulationAge) });
        entity.Write(tag);
        entity.Flags.Write(tag);

        tag.Add("Invulnerable", new ByteTag { Value = entity.Invulnerable ? (byte)1 : (byte)0 });
        tag.Add("HeadRotation", new FloatTag { Value = entity.HeadRotation });
        tag.Add("identifier", new StringTag { Value = entity.Type.Identifier });

        ListTag position = new() { ElementType = Nbt.Enums.TagType.Float };
        position.Value.Add(new FloatTag { Value = entity.Position.X });
        position.Value.Add(new FloatTag { Value = entity.Position.Y });
        position.Value.Add(new FloatTag { Value = entity.Position.Z });
        tag.Add("Pos", position);

        ListTag rotation = new() { ElementType = Nbt.Enums.TagType.Float };
        rotation.Value.Add(new FloatTag { Value = entity.Rotation.Y });
        rotation.Value.Add(new FloatTag { Value = entity.Rotation.X });
        tag.Add("Rotation", rotation);

        if (entity.GetTrait<EntityMovementTrait>() is { } movement)
        {
            if (!float.IsFinite(movement.Motion.X) || !float.IsFinite(movement.Motion.Y) || !float.IsFinite(movement.Motion.Z))
                throw new InvalidDataException("Entity motion is invalid.");
            ListTag motion = new() { ElementType = Nbt.Enums.TagType.Float };
            motion.Value.Add(new FloatTag { Value = movement.Motion.X });
            motion.Value.Add(new FloatTag { Value = movement.Motion.Y });
            motion.Value.Add(new FloatTag { Value = movement.Motion.Z });
            tag.Add("Motion", motion);
        }

        tag.Add("HeadOffset", new FloatTag { Value = entity.HeadOffset });
        return tag;
    }

    public Entity Load(CompoundTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        string identifier = tag.Get<StringTag>("identifier")?.Value
            ?? throw new InvalidDataException("Entity identifier is missing.");

        if (identifier == "minecraft:player")
            throw new InvalidDataException("Load players through the player data store.");

        if (tag.Get<ListTag>("Pos") is not { Value.Count: 3 } position ||
            tag.Get<ListTag>("Rotation") is not { Value.Count: 2 } rotation ||
            position.ElementType != Nbt.Enums.TagType.Float || rotation.ElementType != Nbt.Enums.TagType.Float)
            throw new InvalidDataException("Entity position or rotation is invalid.");
        foreach (Tag value in position.Value)
            if (value is not FloatTag number || !float.IsFinite(number.Value))
                throw new InvalidDataException("Entity position is invalid.");
        foreach (Tag value in rotation.Value)
            if (value is not FloatTag number || !float.IsFinite(number.Value))
                throw new InvalidDataException("Entity rotation is invalid.");

        ListTag? motion = tag.Get<ListTag>("Motion");
        if (motion is not null && (motion.Value.Count != 3 || motion.Value.Any(value => value is not FloatTag number || !float.IsFinite(number.Value))))
            throw new InvalidDataException("Entity motion is invalid.");

        Entity entity;
        if (identifier == "minecraft:item") {
            CompoundTag item = tag.Get<CompoundTag>("Item") ?? throw new InvalidDataException("Dropped item data is missing.");
            ItemType itemType = ItemPalette.Find(item.Get<StringTag>("Name")?.Value ?? string.Empty)
                ?? throw new InvalidDataException("Dropped item type is unavailable.");
            byte count = item.Get<ByteTag>("Count")?.Value ?? 0;
            if (count == 0)
                throw new InvalidDataException("Dropped item count is invalid.");

            CompoundTag? itemNbt = item.Get<CompoundTag>("tag");
            if (itemNbt is not null)
                itemNbt.Name = string.Empty;
            entity = new ItemEntity(new ItemStack(itemType, count,
                unchecked((ushort)(item.Get<ShortTag>("Damage")?.Value ?? 0)), itemNbt));
        }
        else if (identifier == "minecraft:falling_block") {
            var block = tag.Get<CompoundTag>("Block") is { } state ? BlockStorage.ReadPermutation(state) : null;
            if (block is null)
                throw new InvalidDataException("Falling block state is unavailable.");
            entity = new FallingBlockEntity(block, 0, 0, 0);
        }
        else {
            entity = new Entity(Get(identifier), tag.Get<FloatTag>("HeadOffset")?.Value ?? 0);
        }
        entity.StorageTag = tag;
        entity.SimulationAge = checked((ulong)(tag.Get<LongTag>("SimulationAge")?.Value ?? 0));
        entity.Position.X = ((FloatTag)position.Value[0]).Value;
        entity.Position.Y = ((FloatTag)position.Value[1]).Value;
        entity.Position.Z = ((FloatTag)position.Value[2]).Value;
        entity.Rotation.X = ((FloatTag)rotation.Value[1]).Value;
        entity.Rotation.Y = ((FloatTag)rotation.Value[0]).Value;
        entity.HeadRotation = tag.Get<FloatTag>("HeadRotation")?.Value ?? entity.Rotation.Y;

        if (!float.IsFinite(entity.HeadRotation))
            throw new InvalidDataException("Entity head rotation is invalid.");
        entity.Invulnerable = tag.Get<ByteTag>("Invulnerable") is { Value: not 0 };
        entity.Flags.Read(tag);
        entity.Read(tag);

        if (motion is not null && entity.GetTrait<EntityMovementTrait>() is { } movement)
        {
            movement.Motion.X = ((FloatTag)motion.Value[0]).Value;
            movement.Motion.Y = ((FloatTag)motion.Value[1]).Value;
            movement.Motion.Z = ((FloatTag)motion.Value[2]).Value;
        }
        return entity;
    }
}
