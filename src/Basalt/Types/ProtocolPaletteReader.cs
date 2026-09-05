namespace Basalt.Core.Types;

using System.Text;
using System.Text.Json;
using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Item;

internal sealed class ProtocolPaletteReader : IDisposable {
    private readonly System.IO.BinaryReader _reader;
    private readonly string[] _strings;
    private readonly JsonElement[] _elements;

    public ProtocolPaletteReader(ReadOnlyMemory<byte> data) {
        if (data.Length < 8 || !data.Span[..8].SequenceEqual("BASPAL01"u8)) {
            throw new InvalidDataException("The protocol palette header is invalid.");
        }

        _reader = new System.IO.BinaryReader(
            new MemoryStream(data.ToArray(), writable: false), Encoding.UTF8);
        _reader.BaseStream.Position = 8;
        _strings = new string[ReadCount()];
        for (int i = 0; i < _strings.Length; i++) {
            _strings[i] = _reader.ReadString();
        }

        int length = ReadCount();
        byte[] json = _reader.ReadBytes(length);
        if (json.Length != length) {
            throw new EndOfStreamException("The protocol palette properties are truncated.");
        }

        using JsonDocument document = JsonDocument.Parse(json);
        _elements = document.RootElement.Clone().EnumerateArray().ToArray();
    }

    public List<BlockTypeData> ReadBlockTypes() => ReadComplete(ReadList(ReadBlockTypeData));
    public List<BlockPermutationData> ReadBlockPermutations() => ReadComplete(ReadList(ReadBlockPermutationData));
    public List<BlockStateData> ReadBlockStates() => ReadComplete(ReadList(ReadBlockStateData));
    public List<ItemTypeData> ReadItemTypes() => ReadComplete(ReadList(ReadItemTypeData));
    public List<EntityTypeData> ReadEntityTypes() => ReadComplete(ReadList(ReadEntityTypeData));

    public Dictionary<string, BlockDropData> ReadBlockDrops() {
        Dictionary<string, BlockDropData> result = ReadDictionary(() => {
            BlockDropData drop = new();
            for (int i = 0, count = ReadCount(); i < count; i++) {
                string tool = ReadString();
                BlockDropToolData entries = new();
                for (int j = 0, toolCount = ReadCount(); j < toolCount; j++) {
                    entries.Add(ReadString(), ReadList(ReadBlockDropEntryData));
                }
                drop.Add(tool, entries);
            }
            return drop;
        });
        return ReadComplete(result);
    }

    public void Dispose() => _reader.Dispose();

    private T ReadComplete<T>(T value) {
        if (_reader.BaseStream.Position != _reader.BaseStream.Length) {
            throw new InvalidDataException("The protocol palette has trailing data.");
        }
        return value;
    }

    private int ReadCount() {
        int count = _reader.ReadInt32();
        if (count < 0 || count > _reader.BaseStream.Length - _reader.BaseStream.Position) {
            throw new InvalidDataException("The protocol palette count is invalid.");
        }
        return count;
    }

    private List<T> ReadList<T>(Func<T> read) {
        int count = ReadCount();
        List<T> values = new(count);
        for (int i = 0; i < count; i++) values.Add(read());
        return values;
    }

    private Dictionary<string, T> ReadDictionary<T>(Func<T> read) {
        int count = ReadCount();
        Dictionary<string, T> values = new(count, StringComparer.Ordinal);
        for (int i = 0; i < count; i++) values.Add(ReadString(), read());
        return values;
    }

    private string ReadString() {
        int index = _reader.ReadInt32();
        if ((uint)index >= (uint)_strings.Length) throw new InvalidDataException("The protocol palette string index is invalid.");
        return _strings[index];
    }

    private JsonElement ReadElement() {
        int index = _reader.ReadInt32();
        if ((uint)index >= (uint)_elements.Length) throw new InvalidDataException("The protocol palette property index is invalid.");
        return _elements[index];
    }

    private object ReadState() => _reader.ReadByte() switch {
        0 => _reader.ReadBoolean(),
        1 => _reader.ReadInt64(),
        2 => ReadString(),
        _ => throw new InvalidDataException("The protocol palette state type is invalid.")
    };

    private BlockTypeData ReadBlockTypeData() => new() {
        Identifier = ReadString(), Components = ReadDictionary(ReadElement), Tags = ReadList(ReadString), States = ReadList(ReadString),
        Air = _reader.ReadBoolean(), Liquid = _reader.ReadBoolean(), Solid = _reader.ReadBoolean(),
        BlastResistance = _reader.ReadSingle(), Brightness = _reader.ReadSingle(), FlameEncouragement = _reader.ReadSingle(),
        Flammability = _reader.ReadSingle(), Friction = _reader.ReadSingle(), Hardness = _reader.ReadSingle(), Opacity = _reader.ReadSingle(),
        Loggable = _reader.ReadBoolean(), MapColor = _reader.ReadBoolean() ? ReadString() : null
    };

    private BlockPermutationData ReadBlockPermutationData() => new() { Identifier = ReadString(), Hash = _reader.ReadInt32(), State = ReadDictionary(ReadState) };
    private BlockDropEntryData ReadBlockDropEntryData() => new() { Identifier = ReadString(), MinAmount = _reader.ReadInt32(), MaxAmount = _reader.ReadInt32(), Chance = _reader.ReadSingle() };
    private BlockStateData ReadBlockStateData() => new() { Identifier = ReadString(), Type = ReadString(), Values = ReadList(ReadElement) };
    private ItemTypeData ReadItemTypeData() => new() { Identifier = ReadString(), Tags = ReadList(ReadString), MaxAmount = _reader.ReadInt32(), ComponentBased = _reader.ReadBoolean(), NetworkId = _reader.ReadBoolean() ? _reader.ReadInt32() : null, ItemVersion = _reader.ReadInt32(), PropertiesPayload = _reader.ReadBoolean() ? ReadElement() : null, Catalog = _reader.ReadBoolean() ? ReadItemCatalogData() : null };
    private ItemCatalogData ReadItemCatalogData() => new() { CategoryName = ReadString(), GroupIdentifier = _reader.ReadBoolean() ? ReadItemGroupIdentifierData() : null };
    private ItemGroupIdentifierData ReadItemGroupIdentifierData() => new() { Icon = ReadString(), Name = ReadString() };
    private EntityTypeData ReadEntityTypeData() => new() { Identifier = ReadString(), Components = ReadList(ReadString), Loot = _reader.ReadBoolean() ? ReadEntityLootData() : null, PropertiesPayload = _reader.ReadBoolean() ? ReadEntityPropertiesPayloadData() : null };
    private EntityLootData ReadEntityLootData() => new() { Table = ReadString() };
    private EntityPropertiesPayloadData ReadEntityPropertiesPayloadData() => new() { Components = ReadDictionary(ReadElement), ComponentGroups = ReadDictionary(() => ReadDictionary(ReadElement)) };
}
