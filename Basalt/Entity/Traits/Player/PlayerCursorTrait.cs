using Basalt.Entity.Container;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Containers;

namespace Basalt.Entity.Traits.PlayerTraits;

/// <summary>
/// This is a container where when u click on an item in an inventory and the item
/// is on your mouse cursor
/// </summary>
public sealed class PlayerCursorTrait : PlayerTrait
{
    public new static string Identifier => "cursor";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    public EntityContainer Container { get; }

    public PlayerCursorTrait(Entity entity) : base(entity)
    {
        Container = new EntityContainer(Player, Containers.ContainerType.Inventory, 1)
        {
            Identifier = 124
        };
    }

    public override void OnSpawn(Basalt.Entity.Traits.Types.EntitySpawnOptions details)
    {
        Container.Update();
    }

    public override EntityTrait Clone(Entity entity)
    {
        PlayerCursorTrait clone = new(entity);
        if (Container.GetItem(0) is { } item)
        {
            clone.Container.SetItem(0, item);
        }

        return clone;
    }

    public override void OnRead(CompoundTag tag)
    {
        CompoundTag? containerTag = tag.Get<CompoundTag>("container");
        if (containerTag is null)
        {
            return;
        }

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("container", Container.Serialize());
    }
}
