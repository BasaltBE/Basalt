namespace Basalt.Core.Blocks;

using System.Diagnostics.CodeAnalysis;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Loot;


public sealed class Block
{
    private readonly List<BlockTrait> _traits = [];

    public BlockType Type { get; }
    public BlockPermutation Permutation { get; private set; }
    public string Identifier => Type.Identifier;

    public Block(BlockType type, BlockPermutation permutation)
    {
        Type = type;
        Permutation = permutation;
        InitializeTraits();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Trait types are registered with constructors preserved.")]
    private void InitializeTraits()
    {
        foreach (Type traitType in Type.Traits.Values)
        {
            if (Activator.CreateInstance(traitType, this) is BlockTrait trait)
            {
                AddTrait(trait);
            }
        }
    }

    public Block(string identifier)
        : this(BlockType.GetOrAir(identifier), BlockType.GetOrAir(identifier).GetPermutation())
    {
    }

    public Block(BlockPermutation permutation)
        : this(permutation.Type, permutation)
    {
    }

    public void SetPermutation(BlockPermutation permutation)
    {
        if (permutation.Type.Identifier != Type.Identifier)
        {
            throw new ArgumentException("Cannot set permutation for a different block type.", nameof(permutation));
        }
        Permutation = permutation;
    }

    public T AddTrait<T>(T trait) where T : BlockTrait
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

    public bool HasTrait<T>() where T : BlockTrait
    {
        return GetTrait<T>() is not null;
    }

    public T? GetTrait<T>() where T : BlockTrait
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

    public void OnPlace(BlockPlaceDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnPlace(details);
        }
    }

    public void OnBreak(BlockBreakDetails details)
    {
        if (details.Player.Gamemode != Gamemode.Creative && details.Player.Dimension is { } dimension)
        {
            List<ItemStack> drops = LootTableManager.GenerateLootFromBlock(this);
            for (int i = 0; i < drops.Count; i++)
            {
                ItemEntity drop = new(drops[i])
                {
                    Position = new Vec3f
                    {
                        X = details.BlockPosition.X + 0.5f,
                        Y = details.BlockPosition.Y + 0.5f,
                        Z = details.BlockPosition.Z + 0.5f
                    }
                };

                drop.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            }
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnBreak(details);
        }
    }

    public void OnInteract(BlockInteractDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnInteract(details);
        }
    }

    public void OnTick(BlockTickDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnTick(details);
        }
    }

    public void OnRandomTick(BlockRandomTickDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnRandomTick(details);
        }
    }

    public void OnLandOn(BlockLandOnDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnLandOn(details);
        }
    }

    public void OnRender(Player.Player player, int x, int y, int z)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnRender(player, x, y, z);
        }
    }

    public BlockTrait? GetTrait(string identifier)
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

    public void WriteTraits(CompoundTag nbt)
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

    public void ReadTraits(CompoundTag nbt)
    {
        ListTag? traitsTag = nbt.Get<ListTag>("traits");
        if (traitsTag is null)
        {
            for (int i = 0; i < _traits.Count; i++)
            {
                _traits[i].OnRead(nbt);
            }

            return;
        }

        foreach (BaseTag tag in traitsTag.Values)
        {
            if (tag is not CompoundTag traitEntry) continue;

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null) continue;

            BlockTrait? trait = GetTrait(identifier);
            if (trait == null)
            {
                if (BlockTraitRegistry.RegisteredTraits.TryGetValue(identifier, out Type? traitType))
                {
                    if (Activator.CreateInstance(traitType, this) is BlockTrait newTrait)
                    {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            trait?.OnRead(traitData);
        }
    }
}







