namespace Basalt.Core.Blocks;

using Basalt.Core.Blocks.Types;
using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Blocks.Components;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Text;


public sealed class BlockPermutation {
    private static ConcurrentDictionary<int, BlockPermutation> _permutations = [];

    public static ConcurrentDictionary<int, BlockPermutation> Permutations => _permutations;
    private const string AirIdentifier = "minecraft:air";

    public const uint HashOffset = 0x811C9DC5;
    public const int BlockStateVersion = 1;

    public int NetworkId { get; }
    public int Index { get; }
    public BlockState State { get; }
    public BlockType Type { get; }
    private string? _query;
    private BlockTypeComponentCollection? _components;
    private CompoundTag? _nbt;
    internal CollisionBox[]? CollisionBoxes;

    public string Query => _query ??= BuildQuery(State);
    public BlockTypeComponentCollection Components => _components ??= new BlockTypeComponentCollection(this);
    public CompoundTag Nbt => _nbt ??= new CompoundTag();

    public bool IsComponentBased => Components.Values.Count > 0;

    public BlockPermutation(int networkId, BlockState state, BlockType type, string? query = null) {
        NetworkId = networkId;
        State = state;
        Type = type;
        Index = type.Permutations.Count;
        _query = query;
    }

    public bool Matches(BlockState state) {
        foreach (KeyValuePair<string, BlockStateValue> pair in state) {
            if (!State.TryGetValue(pair.Key, out BlockStateValue other) || !other.Equals(pair.Value)) {
                return false;
            }
        }

        return true;
    }

    public static BlockPermutation Resolve(string identifier, BlockState? state = null) {
        return BlockType.GetOrAir(identifier).GetPermutation(state);
    }

    public static BlockPermutation Resolve(int networkId, BlockState? state = null) {
        if (Permutations.TryGetValue(networkId, out BlockPermutation? permutation)) {
            return state is null ? permutation : permutation.Type.GetPermutation(state);
        }

        return Resolve(AirIdentifier, state);
    }

    public static BlockPermutation Resolve(BlockIdentifier identifier, BlockState? state = null) {
        return Resolve(identifier.ToIdentifier(), state);
    }

    public static BlockPermutation Create(BlockType type, BlockState? state = null, string? query = null) {
        lock (type.PermutationLock) {
            BlockState sorted = [];
            if (state is not null) {
                List<string> keys = [.. state.Keys];
                keys.Sort(StringComparer.Ordinal);
                for (int i = 0; i < keys.Count; i++) {
                    string key = keys[i];
                    sorted[key] = state[key];
                    type.EnsureState(key);
                }
            }

            int network = Hash(type.Identifier, sorted);
            if (Permutations.TryGetValue(network, out BlockPermutation? existing)) {
                return existing;
            }

            BlockPermutation permutation = new(network, sorted, type, query);
            Permutations[network] = permutation;
            type.RegisterPermutation(permutation);
            return permutation;
        }
    }

    public static void EnsureRegistryCapacity(int capacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (_permutations.IsEmpty) {
            _permutations = new ConcurrentDictionary<int, BlockPermutation>(
                concurrencyLevel: 1,
                capacity: capacity);
        }
    }

    public static CompoundTag ToCompound(BlockPermutation permutation) {
        CompoundTag root = new();
        root.Set("name", new StringTag { Value = permutation.Type.Identifier });
        root.Set("version", new IntTag { Value = BlockStateVersion });

        CompoundTag states = new();
        foreach ((string key, BlockStateValue value) in permutation.State) {
            states.Set(key, CreateTag(value));
        }

        root.Set("states", states);
        return root;
    }

    public static BlockPermutation FromCompound(CompoundTag nbt) {
        StringTag? name = nbt.Get<StringTag>("name");
        CompoundTag? states = nbt.Get<CompoundTag>("states");

        if (name is null) {
            throw new InvalidOperationException("Block permutation is missing the 'name' tag.");
        }

        BlockState state = [];
        if (states is not null) {
            foreach ((string key, BaseTag tag) in states.Values) {
                state[key] = ToBlockStateValue(tag);
            }
        }

        return Resolve(name.Value, state);
    }

    public static int Hash(string identifier, BlockState state) {
        uint hash = HashOffset;
        HashByte(ref hash, 10);
        HashUInt16(ref hash, 0);

        HashByte(ref hash, 8);
        HashNbtString(ref hash, "name");
        HashNbtString(ref hash, identifier);

        HashByte(ref hash, 10);
        HashNbtString(ref hash, "states");

        if (state.Count > 0) {
            List<string> keys = [.. state.Keys];
            keys.Sort(StringComparer.Ordinal);

            for (int i = 0; i < keys.Count; i++) {
                string key = keys[i];
                BlockStateValue value = state[key];
                HashByte(ref hash, value.Kind switch {
                    0 => 3,
                    1 => 8,
                    2 => 1,
                    _ => throw new InvalidOperationException("Unsupported block state value kind.")
                });
                HashNbtString(ref hash, key);
                HashValue(ref hash, value);
            }
        }

        HashByte(ref hash, 0);
        HashByte(ref hash, 0);

        return unchecked((int)hash);
    }

    private static string BuildQuery(BlockState state) {
        if (state.Count == 0) {
            return string.Empty;
        }

        List<string> keys = [.. state.Keys];
        keys.Sort(StringComparer.Ordinal);
        List<string> parts = new(keys.Count);

        for (int i = 0; i < keys.Count; i++) {
            string key = keys[i];
            BlockStateValue value = state[key];
            string valueText = value.Kind switch {
                0 => value.AsNumber().ToString(System.Globalization.CultureInfo.InvariantCulture),
                1 => $"'{value.AsString()}'",
                2 => value.AsBool() ? "true" : "false",
                _ => throw new InvalidOperationException("Unsupported block state kind.")
            };

            parts.Add($"query.block_state('{key}') == {valueText}");
        }

        return string.Join(" && ", parts);
    }

    private static BaseTag CreateTag(BlockStateValue value) {
        return value.Kind switch {
            0 => new IntTag { Value = checked((int)value.AsNumber()) },
            1 => new StringTag { Value = value.AsString() },
            2 => new ByteTag { Value = value.AsBool() ? (sbyte)1 : (sbyte)0 },
            _ => throw new InvalidOperationException("Unsupported block state value kind.")
        };
    }

    private static BlockStateValue ToBlockStateValue(BaseTag tag) {
        return tag switch {
            ByteTag byteTag => byteTag.Value != 0,
            IntTag intTag => intTag.Value,
            StringTag stringTag => stringTag.Value,
            _ => throw new InvalidOperationException($"Unsupported block state tag type: {tag.Type}.")
        };
    }

    private static void HashNbtString(ref uint hash, string value) {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        HashUInt16(ref hash, checked((ushort)byteCount));
        byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try {
            int written = Encoding.UTF8.GetBytes(value, rented);
            HashBytes(ref hash, rented.AsSpan(0, written));
        }
        finally {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static void HashValue(ref uint hash, BlockStateValue value) {
        switch (value.Kind) {
            case 0:
                Span<byte> number = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(number, checked((int)value.AsNumber()));
                HashBytes(ref hash, number);
                return;
            case 1:
                HashNbtString(ref hash, value.AsString());
                return;
            case 2:
                HashByte(ref hash, value.AsBool() ? (byte)1 : (byte)0);
                return;
            default:
                throw new InvalidOperationException("Unsupported block state value kind.");
        }
    }

    private static void HashUInt16(ref uint hash, ushort value) {
        HashByte(ref hash, (byte)value);
        HashByte(ref hash, (byte)(value >> 8));
    }

    private static void HashBytes(ref uint hash, ReadOnlySpan<byte> bytes) {
        for (int i = 0; i < bytes.Length; i++) {
            HashByte(ref hash, bytes[i]);
        }
    }

    private static void HashByte(ref uint hash, byte value) {
        hash ^= value;
        hash += (hash << 1) + (hash << 4) + (hash << 7) + (hash << 8) + (hash << 24);
    }
}







