namespace Basalt.Core.Item;

using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using Basalt.Core.Item.Traits;
using Basalt.Core.Item.Traits.Types;

public sealed class ItemStack {
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

        foreach (Type traitType in Type.Traits.Values)
        {
            if (Activator.CreateInstance(traitType, this) is Traits.ItemTrait trait)
            {
                AddTrait(trait);
            }
        }
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

    public bool CanStackWith(ItemStack other)
    {
        return Type.Identifier == other.Type.Identifier
               && Metadata == other.Metadata
               && HasSameTraits(other);
    }

    public LegacyItem ToNetworkStack()
    {
        LegacyItem descriptor = ItemType.ToNetworkStack(Type, StackSize, Metadata);
        descriptor.ItemStackId = NetworkStackId;
        descriptor.ExtraData = ExtraData;
        return descriptor;
    }

    public static ItemStack FromNetworkStack(LegacyItem descriptor)
    {
        ItemType type = ItemType.GetByNetwork(descriptor.NetworkId)
                        ?? throw new InvalidOperationException($"Unknown item network id '{descriptor.NetworkId}'.");

        return new ItemStack(type, descriptor.StackSize, unchecked((uint)descriptor.Metadata), descriptor.ExtraData);
    }

    public static ItemStack Empty()
    {
        return new ItemStack(ItemType.Air, 0, 0);
    }

    public CompoundTag Serialize()
    {
        CompoundTag tag = new();
        tag.Set("id", new StringTag { Value = Identifier });
        tag.Set("count", new IntTag { Value = StackSize });
        tag.Set("meta", new IntTag { Value = unchecked((int)Metadata) });

        CompoundTag? nbt = GetSerializedNbt();
        if (nbt is not null && nbt.Values.Count > 0)
        {
            tag.Set("nbt", nbt);
        }

        return tag;
    }

    public static ItemStack? Deserialize(CompoundTag tag)
    {
        StringTag? idTag = tag.Get<StringTag>("id");
        if (idTag is null || string.IsNullOrWhiteSpace(idTag.Value))
        {
            return null;
        }

        ItemType? type = ItemType.Get(idTag.Value);
        if (type is null)
        {
            return null;
        }

        ushort stackSize = (ushort)Math.Max(0, tag.Get<IntTag>("count")?.Value ?? 0);
        uint metadata = unchecked((uint)(tag.Get<IntTag>("meta")?.Value ?? 0));
        CompoundTag? nbt = tag.Get<CompoundTag>("nbt");
        ItemInstanceUserData? extraData = null;
        if (nbt is not null)
        {
            extraData = new ItemInstanceUserData
            {
                Nbt = nbt,
                CanPlaceOn = [],
                CanDestroy = [],
                Ticking = null
            };
        }

        ItemStack stack = new(type, stackSize, metadata, extraData);
        if (nbt is not null)
        {
            stack.ReadTraits(nbt);
        }
        return stack;
    }

    public T AddTrait<T>(T trait) where T : Traits.ItemTrait
    {
        ArgumentNullException.ThrowIfNull(trait);
        if (GetTrait(trait.Identifier) is not null)
        {
            return trait;
        }

        _traits.Add(trait);
        trait.OnAdd();
        return trait;
    }

    public CompoundTag? GetSerializedNbt()
    {
        CompoundTag nbt = ExtraData?.Nbt ?? new CompoundTag();
        WriteTraits(nbt);
        return nbt.Values.Count > 0 ? nbt : null;
    }

    public ItemStack Clone(ushort? stackSize = null)
    {
        CompoundTag serialized = Serialize();
        if (stackSize.HasValue)
        {
            serialized.Set("count", new IntTag { Value = stackSize.Value });
        }

        return Deserialize(serialized) ?? throw new InvalidOperationException("Failed to clone item stack.");
    }

    public void OnUseOnAir(ItemUseOnAirDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnAir(details);
        }
    }

    public void OnUseOnBlock(ItemUseOnBlockDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnBlock(details);
        }
    }

    public void OnPlace(ItemPlaceDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnPlace(details);
        }
    }

    public void OnUseOnEntity(ItemUseOnEntityDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnEntity(details);
        }
    }

    public void OnUseAttack(ItemUseAttackDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseAttack(details);
        }
    }

    public void OnBreakBlock(ItemBreakBlockDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnBreakBlock(details);
        }
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

    private void WriteTraits(CompoundTag nbt)
    {
        if (_traits.Count == 0) return;

        ListTag traitsTag = new() { Name = "traits" };
        foreach (var trait in _traits)
        {
            CompoundTag traitEntry = new();
            traitEntry.Set("id", new StringTag { Value = trait.Identifier });

            CompoundTag traitData = new();
            trait.OnWrite(traitData);
            traitEntry.Set("data", traitData);

            traitsTag.Values.Add(traitEntry);
        }

        nbt.Set("traits", traitsTag);
    }

    private void ReadTraits(CompoundTag nbt)
    {
        ListTag? traitsTag = nbt.Get<ListTag>("traits");
        if (traitsTag is null) return;

        foreach (BaseTag tag in traitsTag.Values)
        {
            if (tag is not CompoundTag traitEntry) continue;

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null) continue;

            Traits.ItemTrait? trait = GetTrait(identifier);
            if (trait == null)
            {
                if (ItemTraitRegistry.RegisteredTraits.TryGetValue(identifier, out Type? traitType))
                {
                    if (Activator.CreateInstance(traitType, this) is Traits.ItemTrait newTrait)
                    {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            trait?.OnRead(traitData);
        }
    }

    // AddTrait methods are now defined above

    public Traits.ItemTrait? GetTrait(string identifier)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (string.Equals(_traits[i].Identifier, identifier, StringComparison.Ordinal))
            {
                return _traits[i];
            }
        }

        return null;
    }

    private bool HasSameTraits(ItemStack other)
    {
        if (_traits.Count != other._traits.Count)
        {
            return false;
        }

        HashSet<string> thisTraits = new(_traits.Count, StringComparer.Ordinal);
        for (int i = 0; i < _traits.Count; i++)
        {
            thisTraits.Add(_traits[i].Identifier);
        }

        for (int i = 0; i < other._traits.Count; i++)
        {
            if (!thisTraits.Contains(other._traits[i].Identifier))
            {
                return false;
            }
        }

        return true;
    }
}






