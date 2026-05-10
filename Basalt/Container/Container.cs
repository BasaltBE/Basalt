using Basalt.Core;
using Basalt.Item;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Containers;

public class Container
{
    private sealed class PacketSuppressionScope : IDisposable
    {
        public void Dispose()
        {
            if (_packetSuppressionDepth > 0)
            {
                _packetSuppressionDepth--;
            }
        }
    }

    private static int _packetSuppressionDepth;
    private readonly Dictionary<Player, int> _occupants = [];
    private static int _nextContainerId = 1;

    public ContainerType Type { get; }
    public int? Identifier { get; set; }
    public List<ItemStack?> Storage { get; private set; }

    public int EmptySlotsCount => Storage.Count(static item => item is null);
    public bool IsFull => EmptySlotsCount == 0;

    public Container(ContainerType type, int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Type = type;
        Storage = Enumerable.Repeat<ItemStack?>(null, size).ToList();
    }

    public int GetSize()
    {
        return Storage.Count;
    }

    public void SetSize(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (size == Storage.Count)
        {
            return;
        }

        List<ItemStack?> resized = Enumerable.Repeat<ItemStack?>(null, size).ToList();
        int copy = Math.Min(size, Storage.Count);
        for (int i = 0; i < copy; i++)
        {
            resized[i] = Storage[i];
        }

        Storage = resized;
        Update();
    }

    public ItemStack? GetItem(int slot)
    {
        slot = NormalizeSlot(slot);
        return Storage[slot];
    }

    public virtual void SetItem(int slot, ItemStack item)
    {
        ArgumentNullException.ThrowIfNull(item);
        slot = NormalizeSlot(slot);
        Storage[slot] = item;
        if (item.StackSize == 0)
        {
            Storage[slot] = null;
        }

        UpdateSlot(slot);
    }

    public bool AddItem(ItemStack item)
    {
        ArgumentNullException.ThrowIfNull(item);

        for (int i = 0; i < Storage.Count; i++)
        {
            ItemStack? existing = Storage[i];
            if (existing is null || !existing.Equals(item) || existing.StackSize >= existing.Type.MaxStackSize)
            {
                continue;
            }

            int available = existing.Type.MaxStackSize - existing.StackSize;
            int move = Math.Min(available, item.StackSize);
            existing.IncrementStack((ushort)move);
            item.DecrementStack((ushort)move);
            UpdateSlot(i);
            if (item.StackSize == 0)
            {
                return true;
            }
        }

        int empty = Storage.FindIndex(static x => x is null);
        if (empty == -1)
        {
            return false;
        }

        SetItem(empty, item);
        return true;
    }

    public ItemStack? RemoveItem(int slot, int amount)
    {
        slot = NormalizeSlot(slot);
        if (amount <= 0)
        {
            return null;
        }

        ItemStack? item = Storage[slot];
        if (item is null)
        {
            return null;
        }

        int removed = Math.Min(amount, item.StackSize);
        item.DecrementStack((ushort)removed);
        if (item.StackSize == 0)
        {
            Storage[slot] = null;
        }

        UpdateSlot(slot);
        return item;
    }

    public ItemStack? TakeItem(int slot, int amount)
    {
        slot = NormalizeSlot(slot);
        if (amount <= 0)
        {
            return null;
        }

        ItemStack? source = Storage[slot];
        if (source is null)
        {
            return null;
        }

        int taken = Math.Min(amount, source.StackSize);
        if (taken == source.StackSize)
        {
            Storage[slot] = null;
            UpdateSlot(slot);
            return source;
        }

        source.DecrementStack((ushort)taken);
        UpdateSlot(slot);
        return new ItemStack(source.Type, (ushort)taken, source.Metadata, source.ExtraData);
    }

    public void SwapItems(int slot, int otherSlot, Container? otherContainer = null)
    {
        Container target = otherContainer ?? this;
        slot = NormalizeSlot(slot);
        otherSlot = target.NormalizeSlot(otherSlot);

        ItemStack? a = GetItem(slot);
        ItemStack? b = target.GetItem(otherSlot);

        Storage[slot] = b;
        target.Storage[otherSlot] = a;

        UpdateSlot(slot);
        target.UpdateSlot(otherSlot);
    }

    public virtual void ClearSlot(int slot)
    {
        slot = NormalizeSlot(slot);
        Storage[slot] = null;
        UpdateSlot(slot);
    }

    public virtual void Clear()
    {
        for (int i = 0; i < Storage.Count; i++)
        {
            Storage[i] = null;
        }

        Update();
    }

    public virtual void UpdateSlot(int slot)
    {
        if (_packetSuppressionDepth > 0)
        {
            return;
        }

        if (Storage.Count == 0)
        {
            return;
        }

        slot = NormalizeSlot(slot);
        foreach ((Player player, int windowId) in _occupants)
        {
            if (!CanSendContainerPackets(player))
            {
                continue;
            }

            SendSlotTo(player, windowId, slot);
        }
    }

    public virtual void Update()
    {
        if (_packetSuppressionDepth > 0)
        {
            return;
        }

        foreach ((Player player, int windowId) in _occupants)
        {
            if (!CanSendContainerPackets(player))
            {
                continue;
            }

            SendContentTo(player, windowId);
        }
    }

    public virtual int Show(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (_occupants.TryGetValue(player, out int existing))
        {
            return existing;
        }

        int id = Identifier ?? _nextContainerId++;
        _occupants[player] = id;
        player.RegisterOpenContainer(id, this);
        if (ShouldSendContainerOpen(player, id))
        {
            ContainerOpenPacket openPacket = new()
            {
                WindowId = (byte)id,
                ContainerType = unchecked((byte)(int)Type),
                ContainerPosition = GetContainerPosition(),
                ContainerEntityUniqueId = GetContainerEntityUniqueId()
            };
            if (CanSendContainerPackets(player))
            {
                player.Send(openPacket);
            }
        }

        Update();
        return id;
    }

    public virtual void Close(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!_occupants.Remove(player, out int id))
        {
            return;
        }
        player.UnregisterOpenContainer(id);

        ContainerClosePacket packet = new()
        {
            WindowId = (byte)id,
            ContainerType = unchecked((byte)(int)Type),
            ServerSide = true
        };
        if (CanSendContainerPackets(player))
        {
            player.Send(packet);
        }
    }

    public bool RemoveOccupant(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!_occupants.Remove(player, out int id))
        {
            return false;
        }

        player.UnregisterOpenContainer(id);
        return true;
    }

    public IReadOnlyCollection<KeyValuePair<Player, int>> GetAllOccupants()
    {
        return _occupants;
    }

    public CompoundTag Serialize()
    {
        CompoundTag root = new();
        root.Set("size", new IntTag { Value = GetSize() });

        ListTag items = new() { Name = "items" };
        for (int slot = 0; slot < GetSize(); slot++)
        {
            ItemStack? item = GetItem(slot);
            if (item is null || item.StackSize == 0)
            {
                continue;
            }

            CompoundTag entry = item.Serialize();
            entry.Set("slot", new IntTag { Value = slot });
            items.Values.Add(entry);
        }

        root.Set("items", items);
        return root;
    }

    public void Deserialize(CompoundTag root)
    {
        int size = root.Get<IntTag>("size")?.Value ?? GetSize();
        if (size != GetSize())
        {
            SetSize(size);
        }

        Clear();
        ListTag? items = root.Get<ListTag>("items");
        if (items is null)
        {
            return;
        }

        for (int i = 0; i < items.Values.Count; i++)
        {
            if (items.Values[i] is not CompoundTag itemTag)
            {
                continue;
            }

            int slot = itemTag.Get<IntTag>("slot")?.Value ?? -1;
            if (slot < 0 || slot >= GetSize())
            {
                continue;
            }

            ItemStack? item = ItemStack.Deserialize(itemTag);
            if (item is null || item.StackSize == 0)
            {
                continue;
            }

            SetItem(slot, item);
        }
    }

    private int NormalizeSlot(int slot)
    {
        if (Storage.Count == 0)
        {
            throw new InvalidOperationException("Container has no slots.");
        }

        int normalized = slot % Storage.Count;
        if (normalized < 0)
        {
            normalized += Storage.Count;
        }

        return normalized;
    }

    protected virtual BlockPos GetContainerPosition()
    {
        return new BlockPos
        {
            X = 0,
            Y = 0,
            Z = 0
        };
    }

    protected virtual long GetContainerEntityUniqueId()
    {
        return -1;
    }

    protected virtual bool ShouldSendContainerOpen(Player player, int windowId)
    {
        return true;
    }

    protected virtual byte GetFullContainerNameId()
    {
        return Type == ContainerType.Inventory ? (byte)0x1B : (byte)7;
    }

    protected static NetworkItemStackDescriptor ToNetworkItem(ItemStack? item)
    {
        if (item is null || item.Type.NetworkId == 0 || item.StackSize == 0)
        {
            return new NetworkItemStackDescriptor();
        }

        int networkBlockId = 0;
        if (item.Type.BlockType is not null && item.Type.BlockType.Permutations.Count > 0)
        {
            networkBlockId = item.Type.BlockType.Permutations[0].NetworkId;
        }

        return new NetworkItemStackDescriptor
        {
            NetworkId = item.Type.NetworkId,
            StackSize = item.StackSize,
            Metadata = unchecked((int)item.Metadata),
            ItemStackId = item.NetworkStackId,
            NetworkBlockId = networkBlockId,
            ExtraData = new ItemInstanceUserData
            {
                Nbt = null,
                CanPlaceOn = [],
                CanDestroy = [],
                Ticking = null
            }
        };
    }

    protected void SendContentTo(Player player, int windowId)
    {
        InventoryContentPacket packet = new()
        {
            WindowId = windowId,
            Content = new List<NetworkItemStackDescriptor>(Storage.Count),
            Container = new FullContainerName
            {
                ContainerId = GetFullContainerNameId(),
                DynamicContainerId = new OptionalValue<uint>
                {
                    HasValue = false
                }
            },
            StorageItem = new NetworkItemStackDescriptor()
        };

        for (int i = 0; i < Storage.Count; i++)
        {
            packet.Content.Add(ToNetworkItem(Storage[i]));
        }

        player.Send(packet);
    }

    protected void SendSlotTo(Player player, int windowId, int slot)
    {
        slot = NormalizeSlot(slot);
        InventorySlotPacket packet = new()
        {
            WindowId = windowId,
            Slot = slot,
            Container = new FullContainerName
            {
                ContainerId = GetFullContainerNameId(),
                DynamicContainerId = new OptionalValue<uint>
                {
                    HasValue = false
                }
            },
            StorageItem = new NetworkItemStackDescriptor(),
            NewItem = ToNetworkItem(Storage[slot])
        };

        player.Send(packet);
    }

    private static bool CanSendContainerPackets(Player player)
    {
        return player.Spawned;
    }

    public static IDisposable SuppressPackets()
    {
        _packetSuppressionDepth++;
        return new PacketSuppressionScope();
    }

    public static bool IsPacketSuppressed()
    {
        return _packetSuppressionDepth > 0;
    }
}
