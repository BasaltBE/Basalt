using Basalt.Protocol.Types;

namespace Basalt.Item;

public sealed class ItemStack
{
    private static int _nextNetworkStackId;
    private readonly List<Traits.ItemTrait> _traits = [];

    public ItemType Type { get; }
    public string Identifier => Type.Identifier;
    public ushort StackSize { get; private set; }
    public uint Metadata { get; private set; }
    public int NetworkStackId { get; } = ++_nextNetworkStackId;
    public ItemInstanceUserData? ExtraData { get; private set; }

    public ItemStack(ItemType type, ushort stackSize = 1, uint metadata = 0, ItemInstanceUserData? extraData = null)
    {
        Type = type;
        StackSize = (ushort)Math.Min(stackSize, type.MaxStackSize);
        Metadata = metadata;
        ExtraData = extraData;
    }

    public ItemStack(string identifier, ushort stackSize = 1, uint metadata = 0, ItemInstanceUserData? extraData = null)
        : this(ItemType.Get(identifier) ?? throw new InvalidOperationException($"Unknown item type '{identifier}'."), stackSize, metadata, extraData)
    {
    }

    public void SetStackSize(ushort value)
    {
        StackSize = (ushort)Math.Min(value, Type.MaxStackSize);
    }

    public void IncrementStack(ushort value = 1)
    {
        SetStackSize((ushort)(StackSize + value));
    }

    public void DecrementStack(ushort value = 1)
    {
        StackSize = value >= StackSize ? (ushort)0 : (ushort)(StackSize - value);
    }

    public void SetMetadata(uint value)
    {
        Metadata = value;
    }

    public void SetExtraData(ItemInstanceUserData? extraData)
    {
        ExtraData = extraData;
    }

    public bool Equals(ItemStack other)
    {
        return Type.Identifier == other.Type.Identifier
               && StackSize == other.StackSize
               && Metadata == other.Metadata
               && Equals(ExtraData, other.ExtraData);
    }

    public NetworkItemStackDescriptor ToNetworkStack()
    {
        NetworkItemStackDescriptor descriptor = ItemType.ToNetworkStack(Type, StackSize, Metadata);
        descriptor.ItemStackId = NetworkStackId;
        descriptor.ExtraData = ExtraData;
        return descriptor;
    }

    public static ItemStack FromNetworkStack(NetworkItemStackDescriptor descriptor)
    {
        ItemType type = ItemType.GetByNetwork(descriptor.NetworkId)
                        ?? throw new InvalidOperationException($"Unknown item network id '{descriptor.NetworkId}'.");

        return new ItemStack(type, descriptor.StackSize, descriptor.Metadata, descriptor.ExtraData);
    }

    public static ItemStack Empty()
    {
        return new ItemStack(ItemType.Air, 0, 0);
    }

    public T AddTrait<T>(T trait) where T : Traits.ItemTrait
    {
        ArgumentNullException.ThrowIfNull(trait);
        _traits.Add(trait);
        return trait;
    }

    public bool HasTrait<T>() where T : Traits.ItemTrait
    {
        return GetTrait<T>() is not null;
    }

    public T? GetTrait<T>() where T : Traits.ItemTrait
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is T typed)
            {
                return typed;
            }
        }

        return null;
    }
}
