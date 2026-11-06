using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Nbt.Types;

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
        if (entity.GetType() != typeof(Entity) || entity.Type is null)
            throw new ArgumentException("Save a typed Entity. Specialized entity classes require their own storage format.", nameof(entity));
        entity.SpawnedDimension?.World?.CheckOwnership();

        CompoundTag tag = new();
        entity.Write(tag);
        tag.Add("identifier", new StringTag { Value = entity.Type.Identifier });

        ListTag position = new() { ElementType = Nbt.Enums.TagType.Float };
        position.Value.Add(new FloatTag { Value = entity.Position.X });
        position.Value.Add(new FloatTag { Value = entity.Position.Y });
        position.Value.Add(new FloatTag { Value = entity.Position.Z });
        tag.Add("Pos", position);

        ListTag rotation = new() { ElementType = Nbt.Enums.TagType.Float };
        rotation.Value.Add(new FloatTag { Value = entity.Rotation.X });
        rotation.Value.Add(new FloatTag { Value = entity.Rotation.Y });
        tag.Add("Rotation", rotation);

        if (entity.GetTrait<EntityMovementTrait>() is { } movement)
        {
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
        EntityType type = Get(identifier);

        if (identifier == "minecraft:player")
            throw new InvalidDataException("Load players through the player data store.");

        if (tag.Get<ListTag>("Pos") is not { Value.Count: 3 } position ||
            tag.Get<ListTag>("Rotation") is not { Value.Count: 2 } rotation ||
            position.Value.Concat(rotation.Value).Any(value => value is not FloatTag number || !float.IsFinite(number.Value)))
            throw new InvalidDataException("Entity position or rotation is invalid.");

        ListTag? motion = tag.Get<ListTag>("Motion");
        if (motion is not null && (motion.Value.Count != 3 || motion.Value.Any(value => value is not FloatTag number || !float.IsFinite(number.Value))))
            throw new InvalidDataException("Entity motion is invalid.");

        Entity entity = new(type, tag.Get<FloatTag>("HeadOffset")?.Value ?? 0);
        entity.Position.X = ((FloatTag)position.Value[0]).Value;
        entity.Position.Y = ((FloatTag)position.Value[1]).Value;
        entity.Position.Z = ((FloatTag)position.Value[2]).Value;
        entity.Rotation.X = ((FloatTag)rotation.Value[0]).Value;
        entity.Rotation.Y = ((FloatTag)rotation.Value[1]).Value;
        entity.HeadRotation = entity.Rotation.Y;
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
