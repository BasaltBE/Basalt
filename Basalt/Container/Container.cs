namespace Basalt.Core.Containers;

using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

using Player = Player.Player;

public class Container {

    // A list of all the players that are vewing the container
    public Dictionary<Player, ContainerId> occupants = [];
    private static int _nextContainerId = (int)ContainerId.First;

    public ContainerType Type { get; }
    public ContainerId? Identifier { get; set; }
    public List<ItemStack?> Storage { get; private set; }

    public int EmptySlotsCount => Storage.Count(static item => item is null);
    public bool IsFull => EmptySlotsCount == 0;

    public Container(ContainerType type, int size) {
        if (size < 0) {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Type = type;
        Storage = Enumerable.Repeat<ItemStack?>(null, size).ToList();
    }


    // Returns the size of the container
    public int GetSize() {
        return Storage.Count;
    }

    /// <summary>
    /// Sets the size of the container
    /// Please do not use this unless you know what you're doing
    /// This should inseat be used in constructor!
    /// </summary>
    /// <param name="size"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public void SetSize(int size) {
        if (size < 0) {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        if (size == Storage.Count) {
            return;
        }

        List<ItemStack?> resized = Enumerable.Repeat<ItemStack?>(null, size).ToList();
        int copy = Math.Min(size, Storage.Count);
        for (int i = 0; i < copy; i++) {
            resized[i] = Storage[i];
        }

        Storage = resized;
        Update();
    }


    /// <summary>
    /// Returns the item in the slot 
    /// But cann also return null
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    public ItemStack? GetItem(int slot) {
        if (slot < 0 || slot >= Storage.Count) {
            return null;
        }
        return Storage[slot];
    }

    /// <summary>
    /// Set an item to a slot.
    /// Will remove the item if the stack size is 0 
    /// but just use the RemoveItem instead if you want to remove the item
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="item"></param>
    public virtual void SetItem(int slot, ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);
        if (slot < 0 || slot >= Storage.Count) {
            return;
        }

        Storage[slot] = item;
        if (item.StackSize == 0) {
            Storage[slot] = null;
        }

        UpdateSlot(slot);
    }

    /// <summary>
    /// Add an item to the container, this checks for slots and counts
    /// so when there is a spot it can put it in it will add, but if there
    /// are no empty slots or stackable slots it will return false
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool AddItem(ItemStack item) {
        ArgumentNullException.ThrowIfNull(item);

        for (int i = 0; i < Storage.Count; i++) {
            ItemStack? existing = Storage[i];
            if (existing is null || !existing.CanStackWith(item) || existing.StackSize >= existing.Type.MaxStackSize) {
                continue;
            }

            int available = existing.Type.MaxStackSize - existing.StackSize;
            int move = Math.Min(available, item.StackSize);
            existing.IncrementStack((ushort)move);
            item.DecrementStack((ushort)move);
            UpdateSlot(i);
            if (item.StackSize == 0) {
                return true;
            }
        }

        int empty = Storage.FindIndex(static x => x is null);
        if (empty == -1) {
            return false;
        }

        SetItem(empty, item);
        return true;
    }


    /// <summary>
    ///  Removes an item from the container
    /// Returns the removed item or null 
    /// Depending on how many you remove
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public ItemStack? RemoveItem(int slot, int amount) {
        if (slot < 0 || slot >= Storage.Count) {
            return null;
        }
        if (amount <= 0) {
            return null;
        }

        ItemStack? item = Storage[slot];
        if (item is null) {
            return null;
        }

        int removed = Math.Min(amount, item.StackSize);
        item.DecrementStack((ushort)removed);
        if (item.StackSize == 0) {
            Storage[slot] = null;
        }

        UpdateSlot(slot);
        return item;
    }

    /// <summary>
    /// Takes an item from the container
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public ItemStack? TakeItem(int slot, int amount) {
        if (slot < 0 || slot >= Storage.Count) {
            return null;
        }
        if (amount <= 0) {
            return null;
        }

        ItemStack? source = Storage[slot];
        if (source is null) {
            return null;
        }

        int taken = Math.Min(amount, source.StackSize);
        if (taken == source.StackSize) {
            Storage[slot] = null;
            UpdateSlot(slot);
            return source;
        }

        source.DecrementStack((ushort)taken);
        UpdateSlot(slot);
        return source.Clone((ushort)taken);
    }

    /// <summary>
    /// Swaps items in the container
    /// pretty self explanatory
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="otherSlot"></param>
    /// <param name="otherContainer"></param>
    public void SwapItems(int slot, int otherSlot, Container? otherContainer = null) {
        Container target = otherContainer ?? this;

        if (slot < 0 || slot >= Storage.Count
            || otherSlot < 0 || otherSlot >= target.Storage.Count) {
            return;
        }

        ItemStack? a = GetItem(slot);
        ItemStack? b = target.GetItem(otherSlot);

        Storage[slot] = b;
        target.Storage[otherSlot] = a;

        UpdateSlot(slot);
        target.UpdateSlot(otherSlot);
    }

    /// <summary>
    /// Clears an item from the container without ifs or buts
    /// </summary>
    /// <param name="slot"></param>
    public virtual void ClearSlot(int slot) {
        if (slot < 0 || slot >= Storage.Count) {
            return;
        }

        Storage[slot] = null;
        UpdateSlot(slot);
    }

    /// <summary>
    /// Clears the whole container of any items
    /// </summary>
    public virtual void Clear() {
        for (int i = 0; i < Storage.Count; i++) {
            Storage[i] = null;
        }

        Update();
    }

    /// <summary>
    /// Updates a single slot of the container and sends it off to player
    /// if they are in the container 
    /// </summary>
    /// <param name="slot"></param>
    public virtual void UpdateSlot(int slot) {
        if (Storage.Count == 0) {
            return;
        }

        if (slot < 0 || slot >= Storage.Count) {
            return;
        }

        foreach ((Player player, ContainerId containerId) in occupants) {
            if (!player.Spawned) {
                continue;
            }

            InventorySlotPacket packet = new() {
                ContainerId = containerId,
                Slot = slot,
                Container = new Optional<FullContainerName> {
                    HasValue = true,
                    Value = GetFullContainerName(containerId)
                },
                NewItem = Storage[slot]?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
            };

            player.Send(packet);
        }
    }

    /// <summary>
    /// Updates the whole container and sends it to occupants
    /// </summary>
    public virtual void Update() {
        foreach ((Player player, ContainerId containerId) in occupants) {
            if (!player.Spawned) {
                continue;
            }

            InventoryContentPacket packet = new() {
                ContainerId = containerId,
                Content = new List<NetworkItemStackDescriptor>(Storage.Count),
                Container = GetFullContainerName(containerId),
                StorageItem = new NetworkItemStackDescriptor()
            };

            for (int i = 0; i < Storage.Count; i++) {
                packet.Content.Add(Storage[i]?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
            }

            player.Send(packet);
        }
    }

    /// <summary>
    /// Shows the container to the player
    /// and returns the window id
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public virtual ContainerId Show(Player player) {
        ArgumentNullException.ThrowIfNull(player);
        if (occupants.TryGetValue(player, out ContainerId existing)) {
            if (player.Spawned && CanOpen(player, existing)) {
                ContainerOpenPacket openPacket = new() {
                    ContainerId = existing,
                    ContainerType = unchecked((byte)(int)Type),
                    ContainerPosition = GetContainerPosition(),
                    ContainerEntityUniqueId = GetContainerEntityUniqueId()
                };

                player.Send(openPacket);
                Update();
            }

            return existing;
        }

        ContainerId id = Identifier ?? (ContainerId)_nextContainerId++;
        if (_nextContainerId > (int)ContainerId.Last) {
            _nextContainerId = (int)ContainerId.First;
        }

        occupants[player] = id;
        player.RegisterOpenContainer(id, this);
        OnViewerAdded(player, id);
        if (CanOpen(player, id)) {
            ContainerOpenPacket openPacket = new() {
                ContainerId = id,
                ContainerType = unchecked((byte)(int)Type),
                ContainerPosition = GetContainerPosition(),
                ContainerEntityUniqueId = GetContainerEntityUniqueId()
            };
            if (player.Spawned) {
                player.Send(openPacket);
            }
        }

        Update();
        return id;
    }

    /// <summary>
    /// Closes the container,
    /// Done as Server to Client.
    /// </summary>
    /// <param name="player"></param>
    public virtual void Close(Player player) {
        ArgumentNullException.ThrowIfNull(player);
        _ = RemoveViewer(player, true);
    }

    public IReadOnlyCollection<KeyValuePair<Player, ContainerId>> GetAllOccupants() {
        return occupants;
    }

    /// <summary>
    /// Serializes the container into nbt CompoundTag,
    /// public only so u can override it to store somewhere else
    /// </summary>
    /// <returns></returns>
    public CompoundTag Serialize() {
        CompoundTag root = new();
        root.Set("size", new IntTag { Value = GetSize() });

        ListTag items = new() { Name = "items" };
        for (int slot = 0; slot < GetSize(); slot++) {
            ItemStack? item = GetItem(slot);
            if (item is null || item.StackSize == 0) {
                continue;
            }

            CompoundTag entry = item.Serialize();
            entry.Set("slot", new IntTag { Value = slot });
            items.Values.Add(entry);
        }

        root.Set("items", items);
        return root;
    }

    /// <summary>
    /// Vise versa of Serialize
    /// Deserializes the container data
    /// </summary>
    /// <param name="root"></param>
    public void Deserialize(CompoundTag root) {
        int size = root.Get<IntTag>("size")?.Value ?? GetSize();
        if (size != GetSize()) {
            SetSize(size);
        }

        Clear();
        ListTag? items = root.Get<ListTag>("items");
        if (items is null) {
            return;
        }

        for (int i = 0; i < items.Values.Count; i++) {
            if (items.Values[i] is not CompoundTag itemTag) {
                continue;
            }

            int slot = itemTag.Get<IntTag>("slot")?.Value ?? -1;
            if (slot < 0 || slot >= GetSize()) {
                continue;
            }

            ItemStack? item = ItemStack.Deserialize(itemTag);
            if (item is null || item.StackSize == 0) {
                continue;
            }

            SetItem(slot, item);
        }
    }

    protected virtual long GetContainerEntityUniqueId() {
        return -1;
    }

    protected virtual bool CanOpen(Player player, ContainerId containerId) {
        return true;
    }

    public bool RemoveViewer(Player player, bool sendClosePacket) {
        ArgumentNullException.ThrowIfNull(player);
        if (!occupants.Remove(player, out ContainerId id)) {
            return false;
        }

        player.openedContainers.Remove(id);
        OnViewerRemoved(player, id);

        if (!sendClosePacket) {
            return true;
        }

        ContainerClosePacket packet = new() {
            ContainerId = id,
            ContainerType = unchecked((byte)(int)Type),
            ServerSide = true
        };
        if (player.Spawned) {
            player.Send(packet);
        }

        return true;
    }

    protected virtual BlockPos GetContainerPosition() {
        return new BlockPos {
            X = 0,
            Y = 0,
            Z = 0
        };
    }

    protected virtual byte GetFullContainerId() {
        return Type == ContainerType.Inventory ? (byte)ContainerName.Inventory : (byte)ContainerName.LevelEntity;
    }

    protected FullContainerName GetFullContainerName(ContainerId containerId) {
        FullContainerName name = new() {
            ContainerId = GetFullContainerId()
        };

        if (Type != ContainerType.Inventory) {
            name.DynamicContainerId = (uint)(byte)containerId;
        }

        return name;
    }

    protected virtual void OnViewerAdded(Player player, ContainerId containerId) {
    }

    protected virtual void OnViewerRemoved(Player player, ContainerId containerId) {
    }
}


