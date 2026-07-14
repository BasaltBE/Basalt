namespace Basalt.Core.Blocks;

using System.Diagnostics.CodeAnalysis;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Item;
using Basalt.Core.Loot;
using Basalt.Core.Worlds;


public sealed class Block
{
    private readonly List<BlockTrait> _traits = [];
    private readonly Dictionary<string, BlockComponent> _components = new(StringComparer.Ordinal);
    private List<ItemStack>? _customDrops;

    public BlockType Type { get; }
    public BlockPermutation Permutation { get; private set; }
    public string Identifier => Type.Identifier;

    public Block(BlockType type, BlockPermutation permutation)
    {
        Type = type;
        Permutation = permutation;
        InitializeComponents();
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

    private void InitializeComponents()
    {
        foreach (BlockComponent typeComponent in Type.GetComponents())
        {
            BlockComponent instance = typeComponent.Clone();
            _components[instance.ComponentIdentifier] = instance;
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

    public T? GetComponent<T>() where T : BlockComponent
    {
        foreach (BlockComponent component in _components.Values)
        {
            if (component is T typed)
            {
                return typed;
            }
        }
        return null;
    }

    public BlockComponent? GetComponent(string identifier)
    {
        return _components.TryGetValue(identifier, out BlockComponent? component) ? component : null;
    }

    public bool HasComponent<T>() where T : BlockComponent
    {
        return GetComponent<T>() is not null;
    }

    public bool HasComponent(string identifier)
    {
        return _components.ContainsKey(identifier) || Type.HasComponent(identifier);
    }

    public void AddComponent(BlockComponent component)
    {
        _components[component.ComponentIdentifier] = component;
    }

    public bool HasTag(string tag) => Type.HasTag(tag);

    public BlockStateValue? GetState(string key)
    {
        return Permutation.State.TryGetValue(key, out BlockStateValue value) ? value : default(BlockStateValue?);
    }

    public void SetState(string key, BlockStateValue value)
    {
        BlockState state = [];
        foreach ((string k, BlockStateValue v) in Permutation.State)
        {
            state[k] = v;
        }

        state[key] = value;
        SetPermutation(Type.GetPermutation(state));
    }

    /// <summary>
    /// Sets custom drops for this block instance.
    /// Pass null to clear and revert to default behavior.
    /// </summary>
    public void SetDrops(List<ItemStack>? drops)
    {
        _customDrops = drops;
    }

    /// <summary>
    /// Gets the drops for this block. 
    /// </summary>
    public List<ItemStack> GetDrops()
    {
        if (_customDrops is not null)
        {
            return _customDrops;
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            List<ItemStack>? traitDrops = _traits[i].GetCustomDrops(Permutation);
            if (traitDrops is not null)
            {
                return traitDrops;
            }
        }

        List<ItemStack> typeDrops = Type.GenerateDrops();
        if (typeDrops.Count > 0)
        {
            return typeDrops;
        }

        return LootTableManager.GenerateLootFromBlock(this);
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
            ulong currentTick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
            List<ItemStack> drops = GetDrops();

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

                drop.LockPickupUntil(currentTick + 10);
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
        if (_traits.Count > 0)
        {
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

        if (_components.Count > 0)
        {
            CompoundTag componentsTag = new();
            foreach ((string key, BlockComponent component) in _components)
            {
                CompoundTag componentData = new();
                component.OnWrite(componentData);
                if (componentData.Values.Count > 0)
                {
                    componentsTag.Set(key, componentData);
                }
            }

            if (componentsTag.Values.Count > 0)
            {
                nbt.Set("components", componentsTag);
            }
        }
    }

    public void ReadTraits(CompoundTag nbt)
    {
        CompoundTag? componentsTag = nbt.Get<CompoundTag>("components");
        if (componentsTag is not null)
        {
            foreach ((string key, BaseTag value) in componentsTag.Values)
            {
                if (value is not CompoundTag componentData)
                {
                    continue;
                }

                if (_components.TryGetValue(key, out BlockComponent? component))
                {
                    component.OnRead(componentData);
                }
            }
        }

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







