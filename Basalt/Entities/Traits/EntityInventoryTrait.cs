namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Container;
using Basalt.Core.Entities.Traits.Enums;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Item;
using Basalt.Core.Worlds;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Player = Player.Player;
using Basalt.Core.Traits;


public sealed class EntityInventoryTrait : EntityTrait {
    public new static string Identifier => "inventory";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:inventory"];

    public EntityContainer Container { get; }
    public int SelectedSlot { get; private set; }
    public bool Opened { get; private set; }

    public EntityInventoryTrait(Entity entity) : base(entity) {
        bool playerInventory = entity.IsPlayer();

        Container = new EntityContainer(
            entity,
            playerInventory ? ContainerType.Inventory : ContainerType.Container,
            playerInventory ? 36 : 27) {
            Identifier = ContainerId.Inventory
        };
    }

    public ItemStack? GetHeldItem() {
        return Container.GetItem(SelectedSlot);
    }

    public void SetHeldItem(int slot) {
        if (slot >= 0 && slot < Container.GetSize()) {
            SelectedSlot = slot;
        }
    }

    public void Clear() {
        Container.Clear();

        if (Entity is not Player player || !player.Spawned) {
            return;
        }

        InventoryContentPacket packet = new() {
            ContainerId = Container.Identifier ?? ContainerId.Inventory,
            Content = Enumerable.Repeat(new NetworkItemStackDescriptor(), Container.GetSize()).ToList(),
            Container = new FullContainerName { ContainerId = (byte)ContainerName.Inventory },
            StorageItem = new NetworkItemStackDescriptor()
        };

        player.Send(packet);
    }

    public override void OnTick(TraitOnTickDetails details) {
        bool hasViewers = Container.GetAllOccupants().Count > 0;

        if (hasViewers == Opened) {
            return;
        }

        Opened = hasViewers;
    }

    public override void OnAdd() {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)Container.Type);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, Container.GetSize());
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (Entity is Player player && player.Spawned) {
            Show(player);
        }
    }

    public void Show(Player player) {
        Container.Show(player);

        EntityEquipmentTrait? equipment = Entity.GetTrait<EntityEquipmentTrait>();
        equipment?.SyncToPlayer(player);
    }
    public override void OnRemove() {
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerType, ActorDataType.Byte, (sbyte)ContainerType.None);
        Entity.Metadata.SetActorMetadata(ActorDataId.ContainerSize, ActorDataType.Int, 0);
    }

    public override void OnInteract(Player player, EntityInteractMethod method) {
        if (method == EntityInteractMethod.Interact && !Entity.IsPlayer()) {
            Container.Show(player);
        }
    }

    public override void OnRead(CompoundTag tag) {
        SelectedSlot = Math.Clamp(
            tag.Get<IntTag>("selected_slot")?.Value ?? SelectedSlot,
            0,
            Container.GetSize() - 1);

        CompoundTag? containerTag = tag.Get<CompoundTag>("container");
        if (containerTag is null) {
            return;
        }

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("selected_slot", new IntTag { Value = SelectedSlot });
        tag.Set("container", Container.Serialize());
    }

    public override void OnRead(CompoundTag entityTag, CompoundTag traitTag) {
        OnRead(traitTag);

        SelectedSlot = Math.Clamp(
            entityTag.Get<IntTag>("SelectedInventorySlot")?.Value ?? SelectedSlot,
            0,
            Container.GetSize() - 1);

        ListTag? oldInventory = entityTag.Get<ListTag>("Inventory");
        if (oldInventory is null) {
            return;
        }

        CompoundTag containerTag = new();
        containerTag.Set("size", new IntTag { Value = Container.GetSize() });

        ListTag items = new() { Name = "items" };

        foreach (BaseTag tag in oldInventory.Values) {
            if (tag is not CompoundTag itemTag) {
                continue;
            }

            int slot = itemTag.Get<IntTag>("Slot")?.Value ?? -1;

            if (slot < 0 || slot >= Container.GetSize()) {
                continue;
            }

            StringTag? id = itemTag.Get<StringTag>("Name");
            if (id is null) {
                continue;
            }

            CompoundTag item = new();

            item.Set("slot", new IntTag { Value = slot });
            item.Set("id", new StringTag { Value = id.Value });
            item.Set("count", new IntTag { Value = itemTag.Get<IntTag>("Count")?.Value ?? 1 });
            item.Set("meta", new IntTag { Value = itemTag.Get<IntTag>("Damage")?.Value ?? 0 });

            CompoundTag? nbt = itemTag.Get<CompoundTag>("tag");
            if (nbt is not null) {
                item.Set("nbt", nbt);
            }

            items.Values.Add(item);
        }

        containerTag.Set("items", items);

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag entityTag, CompoundTag traitTag) {
        OnWrite(traitTag);

        ListTag inventory = new() { Name = "Inventory" };

        for (int slot = 0; slot < Container.GetSize(); slot++) {
            ItemStack? item = Container.GetItem(slot);

            if (item is null || item.StackSize == 0) {
                continue;
            }

            CompoundTag entry = new();

            entry.Set("Slot", new IntTag { Value = slot });
            entry.Set("Name", new StringTag { Value = item.Identifier });
            entry.Set("Count", new IntTag { Value = item.StackSize });
            entry.Set("Damage", new IntTag { Value = unchecked((int)item.Metadata) });

            CompoundTag? nbt = item.GetSerializedNbt();
            if (nbt is not null) {
                entry.Set("tag", nbt);
            }

            inventory.Values.Add(entry);
        }

        entityTag.Set("Inventory", inventory);
        entityTag.Set("SelectedInventorySlot", new IntTag { Value = SelectedSlot });
    }

    public override EntityTrait Clone(Entity entity) {
        EntityInventoryTrait clone = new(entity) {
            SelectedSlot = SelectedSlot
        };

        for (int slot = 0; slot < Container.GetSize(); slot++) {
            ItemStack? item = Container.GetItem(slot);

            if (item is not null) {
                clone.Container.SetItem(slot, item);
            }
        }

        return clone;
    }

    public void SyncToPlayer(Player player) {
        if (!player.Spawned) {
            return;
        }

        InventoryContentPacket packet = new() {
            ContainerId = Container.Identifier ?? ContainerId.Inventory,
            Content = new List<NetworkItemStackDescriptor>(Container.GetSize()),
            Container = new FullContainerName { ContainerId = (byte)ContainerName.Inventory },
            StorageItem = new NetworkItemStackDescriptor()
        };

        for (int i = 0; i < Container.GetSize(); i++) {
            packet.Content.Add(Container.GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
        }

        player.Send(packet);
    }

    public bool DropItem(ItemStack item) {
        if (Entity is not Player player) {
            return false;
        }

        if (Entity.Dimension is null || item.StackSize == 0 || item.Type == ItemType.Air) {
            return false;
        }

        if (Entity.Dimension.World?.Server is Server server) {
            var signal = new Events.PlayerItemDropSignal(player, item);
            server.Emit(signal);
            if (!signal.Emit()) {
                return false;
            }
        }

        Vec3f feet = Entity.GetPosition();
        float yaw = MathF.PI / 180f * player.Yaw;
        float pitch = MathF.PI / 180f * player.Pitch;

        ItemEntity drop = new(item) {
            Location = new Vec3f {
                X = feet.X,
                Y = feet.Y + 1.15f,
                Z = feet.Z
            },
            Velocity = new Vec3f {
                X = -MathF.Sin(yaw) * MathF.Cos(pitch) / 3f,
                Y = -MathF.Sin(pitch) / 2f + 0.2f,
                Z = MathF.Cos(yaw) * MathF.Cos(pitch) / 3f
            }
        };

        ulong currentTick = Entity.Dimension.World is Tickable tickable ? tickable.TickValue : 0;
        drop.LockMergeUntil(currentTick + 40);
        drop.LockPickupUntil(currentTick + 40);
        drop.Spawn(Entity.Dimension, new EntitySpawnOptions(InitialSpawn: false));
        return true;
    }

    public ushort CollectItem(ItemStack item) {
        if (item.StackSize == 0) {
            return 0;
        }

        ushort remaining = item.StackSize;
        ushort moved = 0;

        for (int i = 0; i < Container.GetSize() && remaining > 0; i++) {
            ItemStack? existing = Container.GetItem(i);
            if (existing is null || !existing.CanStackWith(item) || existing.StackSize >= existing.Type.MaxStackSize) {
                continue;
            }

            int space = existing.Type.MaxStackSize - existing.StackSize;
            int transfer = Math.Min(space, remaining);
            if (transfer <= 0) {
                continue;
            }

            existing.IncrementStack((ushort)transfer);
            Container.UpdateSlot(i);
            remaining = (ushort)(remaining - transfer);
            moved = (ushort)(moved + transfer);
        }

        for (int i = 0; i < Container.GetSize() && remaining > 0; i++) {
            if (Container.GetItem(i) is not null) {
                continue;
            }

            ushort transfer = (ushort)Math.Min(remaining, item.Type.MaxStackSize);
            ItemStack stack = item.Clone(transfer);
            Container.SetItem(i, stack);
            remaining = (ushort)(remaining - transfer);
            moved = (ushort)(moved + transfer);
        }

        if (moved == 0) {
            return 0;
        }

        item.SetStackSize(remaining);

        if (Entity is Player player) {
            SyncToPlayer(player);
        }

        return moved;
    }
}






