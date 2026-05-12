using Basalt.Block.Types;
using Basalt.Block.Traits;

namespace Basalt.Block;

public sealed class BlockType
{
    private static readonly Dictionary<string, BlockType> Registry = new(StringComparer.Ordinal);
    private readonly HashSet<string> _stateSet = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BlockPermutation> _permutationStateIndex = new(StringComparer.Ordinal);
    private readonly HashSet<string> _booleanStates = new(StringComparer.Ordinal);

    public string Identifier { get; }
    public float Hardness { get; internal set; }
    public List<string> States { get; } = [];
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
