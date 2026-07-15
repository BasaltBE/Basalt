namespace Basalt.Core.Blocks;

using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Item;


public sealed class BlockType
{
    private static readonly Dictionary<string, BlockType> Registry = new(StringComparer.Ordinal);
    private readonly HashSet<string> _stateSet = new(StringComparer.Ordinal);
    private readonly HashSet<string> _componentSet = new(StringComparer.Ordinal);
    private readonly HashSet<string> _tagSet = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BlockPermutation> _permutationStateIndex = new(StringComparer.Ordinal);
    private readonly HashSet<string> _booleanStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BlockComponent> _components = new(StringComparer.Ordinal);
    private List<BlockDrop>? _drops;

    public string Identifier { get; }
    public bool Air { get; internal set; }
    public bool Liquid { get; internal set; }
    public bool Solid { get; internal set; }
    public float BlastResistance { get; internal set; }
    public float Brightness { get; internal set; }
    public float FlameEncouragement { get; internal set; }
    public float Flammability { get; internal set; }
    public float Friction { get; internal set; }
    public float Hardness { get; internal set; }
    public float Opacity { get; internal set; }
    public bool Loggable { get; internal set; }
    public string? MapColor { get; internal set; }
    public List<string> States { get; } = [];
    public List<string> ComponentIdentifiers { get; } = [];
    public List<string> Tags { get; } = [];
    public List<BlockPermutation> Permutations { get; } = [];
    public IReadOnlyDictionary<string, Type> Traits => _traits;
    private readonly Dictionary<string, Type> _traits = new(StringComparer.Ordinal);
    public static IReadOnlyDictionary<string, BlockType> Types => Registry;

    public BlockType(string identifier)
    {
        Identifier = identifier;
        Registry[identifier] = this;
        BlockTraitRegistry.BindTraitsToType(this);
    }

    public static BlockType? Get(string identifier)
    {
        return Registry.TryGetValue(identifier, out BlockType? type) ? type : null;
    }

    public static BlockType GetOrAir(string identifier)
    {
        return Get(identifier) ?? Get("minecraft:air") ?? new BlockType("minecraft:air");
    }

    public static void EnsureRegistryCapacity(int capacity)
    {
        Registry.EnsureCapacity(capacity);
    }

    public void RegisterPermutation(BlockPermutation permutation)
    {
        Permutations.Add(permutation);
        foreach ((string key, BlockStateValue value) in permutation.State)
        {
            if (value.Kind == 2)
            {
                _booleanStates.Add(key);
            }
        }

        _permutationStateIndex[GetPermutationStateKey(permutation.State)] = permutation;
    }

    public void RegisterTrait(Type traitType, string identifier)
    {
        if (!typeof(BlockTrait).IsAssignableFrom(traitType) || traitType.IsAbstract)
        {
            return;
        }

        _traits.TryAdd(identifier, traitType);
    }

    public void EnsureState(string key)
    {
        if (_stateSet.Add(key))
        {
            States.Add(key);
        }
    }

    public void EnsureComponent(string key)
    {
        if (_componentSet.Add(key))
        {
            ComponentIdentifiers.Add(key);
        }
    }

    public void AddComponent(BlockComponent component)
    {
        string key = component.ComponentIdentifier;
        if (_componentSet.Add(key))
        {
            ComponentIdentifiers.Add(key);
        }
        _components[key] = component;
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

    public IEnumerable<BlockComponent> GetComponents()
    {
        return _components.Values;
    }

    public bool HasComponent(string identifier)
    {
        return _componentSet.Contains(identifier);
    }

    public bool HasComponent<T>() where T : BlockComponent
    {
        return GetComponent<T>() is not null;
    }

    public void SetDrops(List<BlockDrop> drops)
    {
        _drops = drops;
    }

    public List<ItemStack> GenerateDrops()
    {
        if (_drops is null || _drops.Count == 0)
        {
            return [];
        }

        List<ItemStack> items = [];
        for (int i = 0; i < _drops.Count; i++)
        {
            BlockDrop drop = _drops[i];
            if (Random.Shared.NextDouble() > drop.Chance)
            {
                continue;
            }

            ItemType? itemType = ItemType.Get(drop.Identifier);
            if (itemType is null || itemType == ItemType.Air)
            {
                continue;
            }

            int count = Random.Shared.Next(drop.Min, drop.Max + 1);
            if (count > 0)
            {
                items.Add(new ItemStack(itemType, checked((ushort)count)));
            }
        }

        return items;
    }

    public void EnsureTag(string key)
    {
        if (_tagSet.Add(key))
        {
            Tags.Add(key);
        }
    }

    public bool HasTag(string tag)
    {
        return _tagSet.Contains(tag);
    }

    public BlockPermutation GetPermutation(BlockState? state = null)
    {
        if (state is null || state.Count == 0)
        {
            if (Permutations.Count > 0)
            {
                return Permutations[0];
            }

            return BlockPermutation.Create(this, new BlockState());
        }

        string key = GetPermutationStateKey(state);
        if (_permutationStateIndex.TryGetValue(key, out BlockPermutation? cached))
        {
            return cached;
        }

        for (int i = 0; i < Permutations.Count; i++)
        {
            if (Permutations[i].Matches(state))
            {
                _permutationStateIndex[key] = Permutations[i];
                return Permutations[i];
            }
        }

        return BlockPermutation.Create(this, state);
    }

    private string GetPermutationStateKey(BlockState state)
    {
        List<string> keys = [.. state.Keys];
        keys.Sort(StringComparer.Ordinal);
        System.Text.StringBuilder builder = new(keys.Count * 24);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            BlockStateValue value = state[key];

            builder.Append(key);
            builder.Append('=');

            if (_booleanStates.Contains(key) && value.Kind == 0)
            {
                builder.Append(value.AsNumber() == 0 ? "false" : "true");
            }
            else
            {
                switch (value.Kind)
                {
                    case 0:
                        builder.Append(value.AsNumber().ToString(System.Globalization.CultureInfo.InvariantCulture));
                        break;
                    case 1:
                        builder.Append(value.AsString());
                        break;
                    case 2:
                        builder.Append(value.AsBool() ? "true" : "false");
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported block state value kind.");
                }
            }

            builder.Append(';');
        }

        return builder.ToString();
    }
}







