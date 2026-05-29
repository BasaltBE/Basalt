using Basalt.Containers;
using Basalt.Entity.Container;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Traits;

namespace Basalt.Entity.Traits;

public sealed class EntityMovementTrait : EntityTrait
{
    public new static string Identifier => "inventory";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:movement"];


    public EntityMovementTrait(Entity entity) : base(entity)
    { }



    // public override void OnTick(TraitOnTickDetails details) {}

    // public override void OnAdd() {}

    // public override void OnSpawn(EntitySpawnOptions details) {}

    // public override void OnRemove() {}

    // public override void OnInteract(Core.Player player, EntityInteractMethod method) {}


    public override EntityTrait Clone(Entity entity)
    {
        return new EntityMovementTrait(entity)
        { };
    }
}
