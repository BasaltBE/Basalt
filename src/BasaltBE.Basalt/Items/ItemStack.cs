using Basalt.Binary;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Items;

public sealed class ItemStack {
    private static int nextNetId;
    private int netId = Interlocked.Increment(ref nextNetId);
    private ushort stackSize;

    internal Container? Container;
    internal int Slot = -1;
    internal int NetId => netId;

    public readonly ItemType Type;
    public uint Metadata { get; private set; }
    public CompoundTag? Nbt;
    public ushort StackSize => stackSize;
    public bool IsStackFull => stackSize >= Type.MaxAmount;

    public ItemStack(ItemType type, ushort stackSize = 1, uint metadata = 0, CompoundTag? nbt = null) {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
        this.stackSize = stackSize;
        Metadata = metadata;
        Nbt = nbt;
    }

    public ItemStack(string identifier, ushort stackSize = 1, uint metadata = 0, CompoundTag? nbt = null)
        : this(
            ItemPalette.Find(identifier) ?? throw new KeyNotFoundException($"Item type '{identifier}' was not found."),
            stackSize,
            metadata,
            nbt
        ) {
    }

    public void SetStackSize(ushort value) {
        value = (ushort)Math.Min(value, Type.MaxAmount);
        if (StackSize == value)
            return;

        SetStackSizeRaw(value);
        Container?.ItemChanged(this, Slot);
    }

    public void SetMetadata(uint value) {
        if (Metadata == value)
            return;

        SetMetadataRaw(value);
        Container?.ItemChanged(this, Slot);
    }

    public void IncrementStack(ushort amount = 1) => SetStackSize(checked((ushort)Math.Min(Type.MaxAmount, stackSize + amount)));

    public void DecrementStack(ushort amount = 1) => SetStackSize(amount >= stackSize ? (ushort)0 : (ushort)(stackSize - amount));

    public ItemStack? Pop(ushort amount = 1) {
        if (amount == 0 || stackSize == 0)
            return null;

        ushort removed = Math.Min(amount, stackSize);
        ItemStack result = Clone(removed);
        DecrementStack(removed);
        return result;
    }

    public bool CanStackWith(ItemStack other) =>
        ReferenceEquals(Type, other.Type) && Metadata == other.Metadata && NbtEquals(Nbt, other.Nbt);

    public NetworkItemStackDescriptor ToNetworkStackDescriptor() {
        if (Type.NetworkId == 0 || stackSize == 0)
            return new NetworkItemStackDescriptor();

        using BinaryStream stream = BinaryStream.Rent(256);
        BinaryWriter writer = stream.GetWriter();
        writer.WriteInt16(Nbt is null ? (short)0 : (short)-1, true);
        if (Nbt is CompoundTag tag) {
            writer.WriteUInt8(1);
            BasaltBE.Nbt.Nbt.Write(tag, ref writer);
        }
        writer.WriteUInt32(0, true);
        writer.WriteUInt32(0, true);

        return new NetworkItemStackDescriptor {
            Id = checked((short)Type.NetworkId),
            StackSize = stackSize,
            AuxValue = Metadata,
            NetIdVariant = netId,
            BlockRuntimeId = unchecked((uint)(Type.PlacementPermutation?.Hash ?? 0)),
            UserData = Convert.ToBase64String(stream.Buffer.Span[..stream.Offset])
        };
    }

    internal void SetStackSizeRaw(ushort value) {
        if (stackSize == value)
            return;
        stackSize = value;
        netId = Interlocked.Increment(ref nextNetId);
    }

    internal void SetMetadataRaw(uint value) {
        if (Metadata == value)
            return;
        Metadata = value;
        netId = Interlocked.Increment(ref nextNetId);
    }

    internal void Attach(Container container, int slot) {
        if (Container is not null && (!ReferenceEquals(Container, container) || Slot != slot))
            throw new InvalidOperationException("An item stack can only belong to one container slot.");

        Container = container;
        Slot = slot;
    }

    internal void Detach() {
        Container = null;
        Slot = -1;
    }

    internal ItemStack Clone(ushort? size = null, bool cloneNbt = true) {
        ushort clonedSize = size ?? stackSize;
        ItemStack clone = new(Type, clonedSize, Metadata, cloneNbt ? CloneNbt(Nbt) : Nbt) {
            netId = clonedSize == stackSize ? netId : Interlocked.Increment(ref nextNetId)
        };
        return clone;
    }

    private static CompoundTag? CloneNbt(CompoundTag? tag) {
        if (tag is null)
            return null;

        using BinaryStream stream = BinaryStream.Rent(256);
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(tag, ref writer);
        int offset = 0;
        return BasaltBE.Nbt.Nbt.Read(stream.Buffer.Span[..stream.Offset], ref offset) as CompoundTag;
    }

    private static bool NbtEquals(CompoundTag? left, CompoundTag? right) {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;

        return CloneNbtBytes(left).AsSpan().SequenceEqual(CloneNbtBytes(right));
    }

    private static byte[] CloneNbtBytes(CompoundTag tag) {
        using BinaryStream stream = BinaryStream.Rent(256);
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(tag, ref writer);
        return stream.Buffer.Span[..stream.Offset].ToArray();
    }
}
