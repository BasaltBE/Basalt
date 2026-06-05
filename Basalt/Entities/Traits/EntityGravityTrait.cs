namespace Basalt.Core.Entities.Traits;

using Basalt.Protocol.Enums;

public sealed class EntityGravityTrait : EntityTrait
{
    public new static string Identifier => "gravity";
    public new static readonly string[] Components = [
        "minecraft:movement.basic", "minecraft:movement.jump", "minecraft:movement"
    ];

    // Player is ignored here cause its client side.
    public new static readonly EntityIdentifier[] Types = [
        EntityIdentifier.Item,
    ];

    public EntityGravityTrait(Entity entity) : base(entity)
    {
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new EntityGravityTrait(entity);
    }
}




