using Basalt.Core;
using Basalt.Item;

namespace Basalt.Containers;

public class Container
{
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
    }

    public virtual void Update()
    {
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
        return id;
    }

    public virtual void Close(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _occupants.Remove(player);
    }

    public IReadOnlyCollection<KeyValuePair<Player, int>> GetAllOccupants()
    {
        return _occupants;
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
}
