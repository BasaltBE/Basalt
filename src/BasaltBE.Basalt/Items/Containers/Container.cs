using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items.Containers;

public class Container {
    private readonly Lock sync = new();
    private readonly ItemStack?[] items;
    private readonly HashSet<Player> viewers = [];
    private readonly HashSet<Player> persistentViewers = [];
    private readonly IReadOnlySet<ItemType>? bannedItems;

    public readonly ContainerId ContainerId;
    public readonly ContainerEnumName ContainerName;
    public readonly ContainerType Type;
    public readonly uint? DynamicId;
    public readonly int SlotOffset;
    public int ContainerSize => items.Length;
    public event Action? FirstViewerOpened;
    public event Action? LastViewerClosed;

    public Container(
        int size,
        ContainerId containerId = ContainerId.Inventory,
        ContainerEnumName containerName = ContainerEnumName.Inventory,
        ContainerType type = ContainerType.Container,
        uint? dynamicId = null,
        int slotOffset = 0,
        IReadOnlySet<ItemType>? bannedItems = null
    ) {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentOutOfRangeException.ThrowIfNegative(slotOffset);
        items = new ItemStack?[size];
        ContainerId = containerId;
        ContainerName = containerName;
        Type = type;
        DynamicId = dynamicId;
        SlotOffset = slotOffset;
        this.bannedItems = bannedItems;
    }

    public bool CanContain(ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        return bannedItems?.Contains(item.Type) != true;
    }

    public ItemStack? GetItem(int slot) {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, items.Length);
        lock (sync)
            return items[slot];
    }

    public ItemStack? GetItem(string identifier) {
        ArgumentNullException.ThrowIfNull(identifier);
        lock (sync) {
            for (int i = 0; i < items.Length; i++) {
                if (items[i]?.Type.Identifier == identifier)
                    return items[i];
            }
        }
        return null;
    }

    public void SetItem(int slot, ItemStack? item) {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, items.Length);

        lock (sync) {
            if (item is not null && !CanContain(item))
                throw new InvalidOperationException($"Container '{ContainerName}' does not accept '{item.Type.Identifier}'.");
            if (item is not null && item.Container is not null &&
                (!ReferenceEquals(item.Container, this) || item.Slot != slot))
                throw new InvalidOperationException("An item stack can only belong to one container slot.");
            if (item is not null && (item.StackSize == 0 || item.StackSize > item.Type.MaxAmount))
                throw new ArgumentOutOfRangeException(nameof(item), "The item stack size is invalid for this slot.");

            ItemStack? previous = items[slot];
            if (ReferenceEquals(previous, item))
                return;

            previous?.Detach();
            items[slot] = item;
            item?.Attach(this, slot);
        }

        UpdateSlot(slot);
    }

    internal void LoadItem(int slot, ItemStack item) {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, items.Length);
        ArgumentNullException.ThrowIfNull(item);
        lock (sync) {
            if (items[slot] is not null || item.StackSize == 0 || item.StackSize > item.Type.MaxAmount)
                return;
            items[slot] = item;
            item.Attach(this, slot);
        }
    }

    public bool CanAddItem(ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Type.MaxAmount <= 0 || !CanContain(item))
            return false;
        int remaining = item.StackSize;
        lock (sync) {
            for (int i = 0; i < items.Length && remaining > 0; i++) {
                ItemStack? existing = items[i];
                if (existing is null)
                    remaining -= item.Type.MaxAmount;
                else if (existing.CanStackWith(item))
                    remaining -= Math.Max(0, item.Type.MaxAmount - existing.StackSize);
            }
        }
        return remaining <= 0;
    }

    public ItemStack? AddItem(ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Type.MaxAmount <= 0 || !CanContain(item))
            return item;
        if (item.Container is not null)
            throw new InvalidOperationException("Remove an item stack from its current slot before adding it.");

        List<int> changedSlots = [];
        int remaining = item.StackSize;
        lock (sync) {
            for (int i = 0; i < items.Length && remaining > 0; i++) {
                ItemStack? existing = items[i];
                if (existing is null || !existing.CanStackWith(item) || existing.StackSize >= item.Type.MaxAmount)
                    continue;

                int moved = Math.Min(remaining, item.Type.MaxAmount - existing.StackSize);
                existing.SetStackSizeRaw((ushort)(existing.StackSize + moved));
                remaining -= moved;
                changedSlots.Add(i);
            }

            for (int i = 0; i < items.Length && remaining > 0; i++) {
                if (items[i] is not null)
                    continue;

                int moved = Math.Min(remaining, item.Type.MaxAmount);
                ItemStack added = item.Clone((ushort)moved);
                added.Attach(this, i);
                items[i] = added;
                remaining -= moved;
                changedSlots.Add(i);
            }
        }

        item.SetStackSizeRaw((ushort)remaining);
        foreach (int slot in changedSlots)
            UpdateSlot(slot);

        return remaining == 0 ? null : item;
    }

    public ItemStack? TakeItem(int slot, ushort amount = 1) {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, items.Length);
        if (amount == 0)
            return null;

        ItemStack? removed;
        lock (sync) {
            ItemStack? existing = items[slot];
            if (existing is null)
                return null;

            ushort taken = Math.Min(amount, existing.StackSize);
            removed = existing.Clone(taken);
            if (taken == existing.StackSize) {
                items[slot] = null;
                existing.Detach();
            }
            else {
                existing.SetStackSizeRaw((ushort)(existing.StackSize - taken));
            }
        }

        UpdateSlot(slot);
        return removed;
    }

    public void Swap(int firstSlot, int secondSlot) {
        ArgumentOutOfRangeException.ThrowIfNegative(firstSlot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(firstSlot, items.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(secondSlot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(secondSlot, items.Length);
        if (firstSlot == secondSlot)
            return;

        lock (sync) {
            ItemStack? first = items[firstSlot];
            ItemStack? second = items[secondSlot];
            if (ReferenceEquals(first, second))
                return;
            first?.Detach();
            second?.Detach();
            (items[firstSlot], items[secondSlot]) = (second, first);
            items[firstSlot]?.Attach(this, firstSlot);
            items[secondSlot]?.Attach(this, secondSlot);
        }

        UpdateSlot(firstSlot);
        UpdateSlot(secondSlot);
    }

    public void Clear() {
        bool changed = false;
        lock (sync) {
            for (int i = 0; i < items.Length; i++) {
                if (items[i] is not ItemStack item)
                    continue;
                item.Detach();
                items[i] = null;
                changed = true;
            }
        }

        if (changed)
            Update();
    }

    public void Update() {
        ItemStack?[] snapshot;
        Player[] recipients;
        lock (sync) {
            snapshot = (ItemStack?[])items.Clone();
            recipients = [.. viewers];
        }

        if (SlotOffset > 0) {
            for (int i = 0; i < snapshot.Length; i++)
                SendSlot(recipients, i, snapshot[i]);
            return;
        }

        InventoryContentPacket packet = new() {
            ContainerId = unchecked((uint)(sbyte)ContainerId),
            Slots = Array.ConvertAll(snapshot, static item => item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()),
            FullContainerName = GetFullContainerName(),
            StorageItem = new NetworkItemStackDescriptor()
        };

        foreach (Player player in recipients)
            player.Network.SendPacket(player, packet);
    }

    public void UpdateSlot(int slot) {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, items.Length);

        ItemStack? item;
        Player[] recipients;
        lock (sync) {
            item = items[slot];
            recipients = [.. viewers];
        }

        SendSlot(recipients, slot, item);
    }

    internal int GetLocalSlot(int networkSlot) => networkSlot - SlotOffset;

    internal int GetNetworkSlot(int slot) => slot + SlotOffset;

    private void SendSlot(Player[] recipients, int slot, ItemStack? item) {
        NetworkItemStackDescriptor descriptor = item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor();
        InventorySlotPacket packet = new() {
            ContainerId = unchecked((byte)(sbyte)(SlotOffset > 0 ? ContainerId.PlayerOnlyUi : ContainerId)),
            Slot = checked((uint)GetNetworkSlot(slot)),
            FullContainerName = GetFullContainerName(),
            Item = descriptor
        };
        foreach (Player player in recipients)
            player.Network.SendPacket(player, packet);
    }

    public bool Open(Player player) {
        ArgumentNullException.ThrowIfNull(player);
        PlayerOpenContainerEvent opening = new(player, this);
        player.Network.Server.Events.Emit(opening);
        if (opening.IsCanceled() || player.Disposed)
            return false;
        bool firstViewer;
        lock (player.Sync) {
            if (player.OpenedContainer is Container opened && !ReferenceEquals(opened, this))
                opened.Close(player);

            lock (sync) {
                firstViewer = viewers.Count == 0;
                viewers.Add(player);
            }
            player.OpenedContainer = this;
        }

        if (firstViewer)
            FirstViewerOpened?.Invoke();
        player.Network.SendPacket(player, new ContainerOpenPacket {
            ContainerId = unchecked((byte)(sbyte)ContainerId),
            ContainerType = unchecked((byte)(sbyte)Type),
            Position = GetPosition(),
            TargetActorId = GetTargetActorId()
        });
        Update(player);
        return true;
    }

    public bool Close(Player player, bool serverInitiated = true) {
        ArgumentNullException.ThrowIfNull(player);
        bool lastViewer = false;
        lock (player.Sync) {
            bool opened = ReferenceEquals(player.OpenedContainer, this);
            lock (sync) {
                if (!viewers.Contains(player) && !opened)
                    return false;
                if (!persistentViewers.Contains(player) && viewers.Remove(player))
                    lastViewer = viewers.Count == 0;
            }

            if (opened)
                player.OpenedContainer = null;
        }

        if (lastViewer)
            LastViewerClosed?.Invoke();
        player.Network.SendPacket(player, new ContainerClosePacket {
            ContainerId = unchecked((byte)(sbyte)ContainerId),
            ContainerType = unchecked((byte)(sbyte)Type),
            ServerInitiatedClose = serverInitiated
        });
        PlayerCloseContainerEvent closed = new(player, this, serverInitiated);
        player.Network.Server.Events.Emit(closed);
        return true;
    }

    public void AddPersistent(Player player) {
        ArgumentNullException.ThrowIfNull(player);
        bool firstViewer;
        lock (sync) {
            firstViewer = viewers.Count == 0;
            persistentViewers.Add(player);
            viewers.Add(player);
        }
        if (firstViewer)
            FirstViewerOpened?.Invoke();
        Update(player);
    }

    public bool RemovePersistence(Player player) {
        ArgumentNullException.ThrowIfNull(player);
        bool lastViewer;
        lock (sync) {
            if (!persistentViewers.Remove(player))
                return false;
            viewers.Remove(player);
            lastViewer = viewers.Count == 0;
        }
        if (lastViewer)
            LastViewerClosed?.Invoke();
        return true;
    }

    internal void CloseViewers() {
        Player[] current;
        lock (sync)
            current = [.. viewers];
        foreach (Player player in current)
            Close(player);
    }

    public FullContainerName GetFullContainerName() => new() {
        ContainerName = ContainerName,
        DynamicId = DynamicId
    };

    internal void ItemChanged(ItemStack item, int slot) {
        lock (sync) {
            if (slot < 0 || slot >= items.Length || !ReferenceEquals(items[slot], item))
                throw new InvalidOperationException("The item stack is no longer in this container slot.");

            if (item.StackSize == 0) {
                items[slot] = null;
                item.Detach();
            }
        }

        UpdateSlot(slot);
    }

    private void Update(Player player) {
        ItemStack?[] snapshot;
        lock (sync)
            snapshot = (ItemStack?[])items.Clone();

        if (SlotOffset > 0) {
            Player[] recipient = [player];
            for (int i = 0; i < snapshot.Length; i++)
                SendSlot(recipient, i, snapshot[i]);
            return;
        }

        player.Network.SendPacket(player, new InventoryContentPacket {
            ContainerId = unchecked((uint)(sbyte)ContainerId),
            Slots = Array.ConvertAll(snapshot, static item => item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()),
            FullContainerName = GetFullContainerName(),
            StorageItem = new NetworkItemStackDescriptor()
        });
    }

    protected virtual BlockPos GetPosition() => new();

    protected virtual long GetTargetActorId() => -1;
}
