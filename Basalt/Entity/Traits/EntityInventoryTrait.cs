using Basalt.Containers;
using Basalt.Entity.Container;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Traits;
using Basalt.Protocol.Nbt;

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
        ContainerType containerType = entity.IsPlayer() ? ContainerType.Inventory : ContainerType.Container;
        int size = entity.IsPlayer() ? 36 : 27;
        Container = new EntityContainer(entity, containerType, size)
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
        if (slot < 0 || slot >= Container.GetSize())
        {
            return;
        }

        SelectedSlot = slot;
    }

    public void OnOpen()
    {
    }

    public void OnClose()
    {
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        bool hasOccupants = Container.GetAllOccupants().Count > 0;
        if (!Opened && hasOccupants)
        {
            Opened = true;
            OnOpen();
        }
        else if (Opened && !hasOccupants)
        {
            Opened = false;
            OnClose();
        }
    }

    public override void OnAdd()
    {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)Container.Type);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, Container.GetSize());
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (Entity is not Core.Player)
        {
            return;
        }

        Container.Update();
    }

    public override void OnRemove()
    {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)Basalt.Containers.ContainerType.None);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, 0);
    }

    public override void OnInteract(Core.Player player, EntityInteractMethod method)
    {
        if (method != EntityInteractMethod.Interact || Entity.IsPlayer())
        {
            return;
        }

        Container.Show(player);
    }

    public override void OnRead(CompoundTag tag)
    {
        SelectedSlot = Math.Clamp(tag.Get<IntTag>("selected_slot")?.Value ?? SelectedSlot, 0, Container.GetSize() - 1);
        CompoundTag? containerTag = tag.Get<CompoundTag>("container");
        if (containerTag is null)
        {
            return;
        }

        using IDisposable _ = Containers.Container.SuppressPackets();
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
        SelectedSlot = Math.Clamp(entityTag.Get<IntTag>("SelectedInventorySlot")?.Value ?? SelectedSlot, 0, Container.GetSize() - 1);

        ListTag? inventoryList = entityTag.Get<ListTag>("Inventory");
        if (inventoryList is null)
        {
            return;
        }

        CompoundTag compatContainerTag = new();
        compatContainerTag.Set("size", new IntTag { Value = Container.GetSize() });
        ListTag items = new() { Name = "items" };

        for (int i = 0; i < inventoryList.Values.Count; i++)
        {
            if (inventoryList.Values[i] is not CompoundTag itemTag)
            {
                continue;
            }

            int slot = itemTag.Get<IntTag>("Slot")?.Value ?? -1;
            if (slot < 0 || slot >= Container.GetSize())
            {
                continue;
            }

            CompoundTag entry = new();
            StringTag? idTag = itemTag.Get<StringTag>("Name");
            IntTag? countTag = itemTag.Get<IntTag>("Count");
            IntTag? metaTag = itemTag.Get<IntTag>("Damage");
            CompoundTag? nbtTag = itemTag.Get<CompoundTag>("tag");

            if (idTag is null)
            {
                continue;
            }

            entry.Set("slot", new IntTag { Value = slot });
            entry.Set("id", new StringTag { Value = idTag.Value });
            entry.Set("count", new IntTag { Value = countTag?.Value ?? 1 });
            entry.Set("meta", new IntTag { Value = metaTag?.Value ?? 0 });
            if (nbtTag is not null)
            {
                entry.Set("nbt", nbtTag);
            }

            items.Values.Add(entry);
        }

        compatContainerTag.Set("items", items);
        using IDisposable _ = Containers.Container.SuppressPackets();
        Container.Deserialize(compatContainerTag);
    }

    public override void OnWrite(CompoundTag entityTag, CompoundTag traitTag)
    {
        OnWrite(traitTag);

        ListTag inventoryList = new() { Name = "Inventory" };
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
            if (item.ExtraData?.Nbt is not null)
            {
                entry.Set("tag", item.ExtraData.Nbt);
            }

            inventoryList.Values.Add(entry);
        }

        entityTag.Set("Inventory", inventoryList);
        entityTag.Set("SelectedInventorySlot", new IntTag { Value = SelectedSlot });
    }

    public override EntityTrait Clone(Entity entity)
    {
        EntityInventoryTrait clone = new(entity);
        clone.SelectedSlot = SelectedSlot;
        for (int i = 0; i < Container.GetSize(); i++)
        {
            if (Container.GetItem(i) is { } item)
            {
                clone.Container.SetItem(i, item);
            }
        }

        return clone;
    }
}
