using Basalt.Binary;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Enums;
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
    public bool Enchanted => Nbt?.Get<ListTag>("ench")?.Value.Count > 0 ||
        Nbt?.Get<ListTag>("basalt:enchantments")?.Value.Count > 0;
    public int Damage => Type.MaxDurability > 0
        ? checked((int)Metadata)
        : Nbt?.Get<ShortTag>("Damage")?.Value ?? Nbt?.Get<IntTag>("Damage")?.Value ?? 0;
    public int RemainingDurability => Math.Max(0, Type.MaxDurability - Damage);

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

    public void SetDamage(int value) {
        if (Type.MaxDurability <= 0)
            throw new InvalidOperationException($"Item '{Type.Identifier}' does not have durability.");
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (value >= Type.MaxDurability) {
            SetStackSize(0);
            return;
        }

        if (Damage == value)
            return;

        SetMetadataRaw((uint)value);
        Nbt ??= new CompoundTag();
        Nbt.Add("Damage", new IntTag { Value = value });
        Container?.ItemChanged(this, Slot);
    }

    public bool DamageDurability(int amount = 1) {
        if (amount <= 0 || Type.MaxDurability <= 0 || stackSize == 0)
            return false;

        int minimumChance = Type.DurabilityDamageChanceMinimum;
        int maximumChance = Type.DurabilityDamageChanceMaximum;
        int unbreaking = GetEnchantmentLevel(EnchantmentPalette.Unbreaking);
        if (unbreaking == 0) {
            int chance = minimumChance == maximumChance
                ? minimumChance
                : Random.Shared.Next(minimumChance, maximumChance + 1);
            if (chance < 100 && Random.Shared.Next(100) >= chance)
                return false;
        }

        int damage = Damage;
        int applied = 0;
        for (int i = 0; i < amount; i++) {
            if (unbreaking > 0) {
                int chance = minimumChance == maximumChance
                    ? minimumChance
                    : Random.Shared.Next(minimumChance, maximumChance + 1);
                if (chance < 100 && Random.Shared.Next(100) >= chance)
                    continue;

                bool wears = Type.ArmorSlot is not null
                    ? Random.Shared.Next(100) < 60 || Random.Shared.Next(unbreaking + 1) == 0
                    : Random.Shared.Next(unbreaking + 1) == 0;
                if (!wears)
                    continue;
            }

            applied++;
        }

        if (applied == 0)
            return false;
        if (applied >= Type.MaxDurability - damage) {
            SetStackSize(0);
            return true;
        }

        SetDamage(damage + applied);
        return false;
    }

    public int GetEnchantmentLevel(Enchantment enchantment) {
        ArgumentNullException.ThrowIfNull(enchantment);
        return GetEnchantmentLevel(enchantment.Identifier, enchantment.BedrockId);
    }

    public void AddEnchantment(Enchantment enchantment, int level) {
        ArgumentNullException.ThrowIfNull(enchantment);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, short.MaxValue);
        if (Type.Identifier is not ("minecraft:book" or "minecraft:enchanted_book") && !enchantment.AppliesTo(Type))
            throw new InvalidOperationException($"Enchantment '{enchantment.Identifier}' does not apply to '{Type.Identifier}'.");

        foreach (string incompatible in enchantment.IncompatibleWith.Span)
            if (GetEnchantmentLevel(incompatible) > 0)
                throw new InvalidOperationException($"Enchantment '{enchantment.Identifier}' is incompatible with '{incompatible}'.");

        if (GetEnchantmentLevel(enchantment) != level)
            WriteEnchantment(enchantment, level);
    }

    public bool RemoveEnchantment(Enchantment enchantment) {
        ArgumentNullException.ThrowIfNull(enchantment);
        if (GetEnchantmentLevel(enchantment) == 0)
            return false;

        WriteEnchantment(enchantment, 0);
        return true;
    }

    public int GetEnchantmentLevel(string identifier) =>
        GetEnchantmentLevel(identifier, EnchantmentPalette.Find(identifier)?.BedrockId);

    private int GetEnchantmentLevel(string identifier, short? bedrockId) {
        if (bedrockId is short nativeId) {
            if (Nbt?.Get<ListTag>("ench") is not { } enchantments)
                return 0;

            foreach (Tag entry in enchantments.Value)
                if (entry is CompoundTag compound &&
                    compound.Get<ShortTag>("id")?.Value == nativeId)
                    return compound.Get<ShortTag>("lvl")?.Value ?? 0;
            return 0;
        }

        if (Nbt?.Get<ListTag>("basalt:enchantments") is not { } customEnchantments)
            return 0;
        foreach (Tag entry in customEnchantments.Value)
            if (entry is CompoundTag compound &&
                string.Equals(compound.Get<StringTag>("id")?.Value, identifier, StringComparison.Ordinal))
                return compound.Get<IntTag>("lvl")?.Value ?? 0;
        return 0;
    }

    private void WriteEnchantment(Enchantment enchantment, int level) {
        if (enchantment.BedrockId is short bedrockId) {
            ListTag? enchantments = Nbt?.Get<ListTag>("ench");
            int index = enchantments?.Value.FindIndex(entry =>
                entry is CompoundTag compound && compound.Get<ShortTag>("id")?.Value == bedrockId) ?? -1;
            if (level == 0) {
                if (index < 0)
                    return;
                enchantments!.Value.RemoveAt(index);
                if (enchantments.Value.Count == 0)
                    Nbt!.Remove("ench");
            }
            else {
                CompoundTag entry = new();
                entry.Add("id", new ShortTag { Value = bedrockId });
                entry.Add("lvl", new ShortTag { Value = checked((short)level) });
                if (index < 0) {
                    enchantments ??= new ListTag { ElementType = TagType.Compound };
                    enchantments.Value.Add(entry);
                    Nbt ??= new CompoundTag();
                    Nbt.Add("ench", enchantments);
                }
                else {
                    enchantments!.Value[index] = entry;
                }
            }
        }
        else {
            ListTag? enchantments = Nbt?.Get<ListTag>("basalt:enchantments");
            int index = enchantments?.Value.FindIndex(entry =>
                entry is CompoundTag compound &&
                string.Equals(compound.Get<StringTag>("id")?.Value, enchantment.Identifier, StringComparison.Ordinal)) ?? -1;
            if (level == 0) {
                if (index < 0)
                    return;
                enchantments!.Value.RemoveAt(index);
                if (enchantments.Value.Count == 0)
                    Nbt!.Remove("basalt:enchantments");
            }
            else {
                CompoundTag entry = new();
                entry.Add("id", new StringTag { Value = enchantment.Identifier });
                entry.Add("lvl", new IntTag { Value = level });
                if (index < 0) {
                    enchantments ??= new ListTag { ElementType = TagType.Compound };
                    enchantments.Value.Add(entry);
                    Nbt ??= new CompoundTag();
                    Nbt.Add("basalt:enchantments", enchantments);
                }
                else {
                    enchantments!.Value[index] = entry;
                }
            }
        }

        netId = Interlocked.Increment(ref nextNetId);
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

        int nbtSize = Nbt?.GetSize() ?? 0;
        using BinaryStream stream = BinaryStream.Rent(sizeof(short) + (nbtSize == 0 ? 0 : sizeof(byte) + nbtSize) + sizeof(uint) * 2);
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

    internal void RefreshNetId() {
        netId = Interlocked.Increment(ref nextNetId);
        Container?.ItemChanged(this, Slot);
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

        using BinaryStream stream = BinaryStream.Rent(tag.GetSize());
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
        using BinaryStream stream = BinaryStream.Rent(tag.GetSize());
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(tag, ref writer);
        return stream.Buffer.Span[..stream.Offset].ToArray();
    }

}
