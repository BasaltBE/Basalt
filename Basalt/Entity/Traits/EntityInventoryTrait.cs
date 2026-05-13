using Basalt.Containers;
using Basalt.Entity.Container;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Traits;

namespace Basalt.Entity.Traits;

public sealed class EntityInventoryTrait : EntityTrait
{
    public new static string Identifier => "inventory";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:inventory"];

    public EntityContainer Container { get; }
    public int SelectedSlot { get; private set; }
    public bool Opened { get; private set; }

    public EntityInventoryTrait(Entity entity) : base(entity)
    {
        bool playerInventory = entity.IsPlayer();

        Container = new EntityContainer(
            entity,
            playerInventory ? ContainerType.Inventory : ContainerType.Container,
            playerInventory ? 36 : 27)
        {
            Identifier = 0
        };
    }

    public ItemStack? GetHeldItem()
    {
        return Container.GetItem(SelectedSlot);
    }

    public void SetHeldItem(int slot)
    {
        if (slot >= 0 && slot < Container.GetSize())
        {
            SelectedSlot = slot;
        }
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        bool hasViewers = Container.GetAllOccupants().Count > 0;

        if (hasViewers == Opened)
        {
            return;
        }

        Opened = hasViewers;
    }

    public override void OnAdd()
    {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)Container.Type);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, Container.GetSize());
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (Entity is Core.Player player)
        {
            Container.Show(player);
            Container.Update();
        }
    }

    public override void OnRemove()
    {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)ContainerType.None);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, 0);
    }

    public override void OnInteract(Core.Player player, EntityInteractMethod method)
    {
        if (method == EntityInteractMethod.Interact && !Entity.IsPlayer())
        {
            Container.Show(player);
        }
    }

    public override void OnRead(CompoundTag tag)
    {
        SelectedSlot = Math.Clamp(
            tag.Get<IntTag>("selected_slot")?.Value ?? SelectedSlot,
            0,
            Container.GetSize() - 1);

        CompoundTag? containerTag = tag.Get<CompoundTag>("container");
        if (containerTag is null)
        {
            return;
        }

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("selected_slot", new IntTag { Value = SelectedSlot });
        tag.Set("container", Container.Serialize());
    }

    public override void OnRead(CompoundTag entityTag, CompoundTag traitTag)
    {
        OnRead(traitTag);

        SelectedSlot = Math.Clamp(
            entityTag.Get<IntTag>("SelectedInventorySlot")?.Value ?? SelectedSlot,
            0,
            Container.GetSize() - 1);

        ListTag? oldInventory = entityTag.Get<ListTag>("Inventory");
        if (oldInventory is null)
        {
            return;
        }

        CompoundTag containerTag = new();
        containerTag.Set("size", new IntTag { Value = Container.GetSize() });

        ListTag items = new() { Name = "items" };

        foreach (BaseTag tag in oldInventory.Values)
        {
            if (tag is not CompoundTag itemTag)
            {
                continue;
            }

            int slot = itemTag.Get<IntTag>("Slot")?.Value ?? -1;

            if (slot < 0 || slot >= Container.GetSize())
            {
                continue;
            }

            StringTag? id = itemTag.Get<StringTag>("Name");
            if (id is null)
            {
                continue;
            }

            CompoundTag item = new();

            item.Set("slot", new IntTag { Value = slot });
            item.Set("id", new StringTag { Value = id.Value });
            item.Set("count", new IntTag { Value = itemTag.Get<IntTag>("Count")?.Value ?? 1 });
            item.Set("meta", new IntTag { Value = itemTag.Get<IntTag>("Damage")?.Value ?? 0 });

            CompoundTag? nbt = itemTag.Get<CompoundTag>("tag");
            if (nbt is not null)
            {
                item.Set("nbt", nbt);
            }

            items.Values.Add(item);
        }

        containerTag.Set("items", items);

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag entityTag, CompoundTag traitTag)
    {
        OnWrite(traitTag);

        ListTag inventory = new() { Name = "Inventory" };

        for (int slot = 0; slot < Container.GetSize(); slot++)
        {
            ItemStack? item = Container.GetItem(slot);

            if (item is null || item.StackSize == 0)
            {
                continue;
            }

            CompoundTag entry = new();

            entry.Set("Slot", new IntTag { Value = slot });
            entry.Set("Name", new StringTag { Value = item.Identifier });
            entry.Set("Count", new IntTag { Value = item.StackSize });
            entry.Set("Damage", new IntTag { Value = unchecked((int)item.Metadata) });

            CompoundTag? nbt = item.GetSerializedNbt();
            if (nbt is not null)
            {
                entry.Set("tag", nbt);
            }

            inventory.Values.Add(entry);
        }

        entityTag.Set("Inventory", inventory);
        entityTag.Set("SelectedInventorySlot", new IntTag { Value = SelectedSlot });
    }

    public override EntityTrait Clone(Entity entity)
    {
        EntityInventoryTrait clone = new(entity)
        {
            SelectedSlot = SelectedSlot
        };

        for (int slot = 0; slot < Container.GetSize(); slot++)
        {
            ItemStack? item = Container.GetItem(slot);

            if (item is not null)
            {
                clone.Container.SetItem(slot, item);
            }
        }

        return clone;
    }
}
