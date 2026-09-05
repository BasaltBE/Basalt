using System.Text;
using System.Text.Json;
using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Item;

internal sealed class ProtocolPaletteWriter : IDisposable {
    private readonly MemoryStream _payload = new();
    private readonly BinaryWriter _writer;
    private readonly Dictionary<string, int> _strings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _elements = new(StringComparer.Ordinal);

    public ProtocolPaletteWriter() {
        _writer = new BinaryWriter(_payload, Encoding.UTF8, leaveOpen: true);
    }

    public byte[] Write(string name, ReadOnlySpan<byte> json) {
        switch (name) {
            case "block-types.json":
                WriteList(JsonSerializer.Deserialize(json, BlockPaletteJsonContext.Default.ListBlockTypeData)
                    ?? throw new InvalidDataException("Block types are missing."), WriteBlockTypeData);
                break;
            case "block_permutations.json":
                WriteList(JsonSerializer.Deserialize(json, BlockPaletteJsonContext.Default.ListBlockPermutationData)
                    ?? throw new InvalidDataException("Block permutations are missing."), WriteBlockPermutationData);
                break;
            case "block-drops.json":
                Dictionary<string, BlockDropData> drops = JsonSerializer.Deserialize(
                    json, BlockPaletteJsonContext.Default.DictionaryStringBlockDropData)
                    ?? throw new InvalidDataException("Block drops are missing.");
                WriteDictionary(drops, drop => {
                    WriteDictionary(drop, tool => {
                        WriteDictionary(tool, entries => WriteList(entries, WriteBlockDropEntryData));
                    });
                });
                break;
            case "block_states.json":
                WriteList(JsonSerializer.Deserialize(json, BlockPaletteJsonContext.Default.ListBlockStateData)
                    ?? throw new InvalidDataException("Block states are missing."), WriteBlockStateData);
                break;
            case "item-types.json":
                WriteList(JsonSerializer.Deserialize(json, ItemPaletteJsonContext.Default.ListItemTypeData)
                    ?? throw new InvalidDataException("Item types are missing."), WriteItemTypeData);
                break;
            case "entity-types.json":
                WriteList(JsonSerializer.Deserialize(json, EntityPaletteJsonContext.Default.ListEntityTypeData)
                    ?? throw new InvalidDataException("Entity types are missing."), WriteEntityTypeData);
                break;
            default:
                throw new InvalidDataException($"Unsupported palette '{name}'.");
        }

        using MemoryStream output = new();
        using BinaryWriter writer = new(output, Encoding.UTF8, leaveOpen: true);
        writer.Write("BASPAL01"u8);
        writer.Write(_strings.Count);
        foreach (string value in _strings.Keys) {
            writer.Write(value);
        }

        using MemoryStream properties = new();
        using (Utf8JsonWriter jsonWriter = new(properties)) {
            jsonWriter.WriteStartArray();
            foreach (string element in _elements.Keys) {
                jsonWriter.WriteRawValue(element);
            }
            jsonWriter.WriteEndArray();
        }
        writer.Write(checked((int)properties.Length));
        writer.Write(properties.GetBuffer().AsSpan(0, checked((int)properties.Length)));
        writer.Write(_payload.GetBuffer().AsSpan(0, checked((int)_payload.Length)));
        return output.ToArray();
    }

    public void Dispose() {
        _writer.Dispose();
        _payload.Dispose();
    }

    private void WriteList<T>(IReadOnlyCollection<T> values, Action<T> write) {
        _writer.Write(values.Count);
        foreach (T value in values) {
            write(value);
        }
    }

    private void WriteDictionary<T>(IReadOnlyDictionary<string, T> values, Action<T> write) {
        _writer.Write(values.Count);
        foreach ((string name, T value) in values) {
            WriteString(name);
            write(value);
        }
    }

    private void WriteString(string value) {
        if (!_strings.TryGetValue(value, out int index)) {
            index = _strings.Count;
            _strings.Add(value, index);
        }
        _writer.Write(index);
    }

    private void WriteElement(JsonElement value) {
        string json = value.GetRawText();
        if (!_elements.TryGetValue(json, out int index)) {
            index = _elements.Count;
            _elements.Add(json, index);
        }
        _writer.Write(index);
    }

    private void WriteState(object value) {
        if (value is not JsonElement element) {
            throw new InvalidDataException("A block state must come from JSON.");
        }
        switch (element.ValueKind) {
            case JsonValueKind.True:
            case JsonValueKind.False:
                _writer.Write((byte)0);
                _writer.Write(element.GetBoolean());
                break;
            case JsonValueKind.Number:
                _writer.Write((byte)1);
                _writer.Write(element.GetInt64());
                break;
            case JsonValueKind.String:
                _writer.Write((byte)2);
                WriteString(element.GetString()!);
                break;
            default:
                throw new InvalidDataException("A block state must be a boolean, integer, or string.");
        }
    }

    private void WriteBlockTypeData(BlockTypeData value) {
        WriteString(value.Identifier);
        WriteDictionary(value.Components, value0 => {
            WriteElement(value0);
        });
        WriteList(value.Tags, value1 => {
            WriteString(value1);
        });
        WriteList(value.States, value2 => {
            WriteString(value2);
        });
        _writer.Write(value.Air);
        _writer.Write(value.Liquid);
        _writer.Write(value.Solid);
        _writer.Write(value.BlastResistance);
        _writer.Write(value.Brightness);
        _writer.Write(value.FlameEncouragement);
        _writer.Write(value.Flammability);
        _writer.Write(value.Friction);
        _writer.Write(value.Hardness);
        _writer.Write(value.Opacity);
        _writer.Write(value.Loggable);
        if (value.MapColor is { } value3) {
            _writer.Write(true);
            WriteString(value3);
        }
        else {
            _writer.Write(false);
        }
    }

    private void WriteBlockPermutationData(BlockPermutationData value) {
        WriteString(value.Identifier);
        _writer.Write(value.Hash);
        WriteDictionary(value.State, value4 => {
            WriteState(value4);
        });
    }

    private void WriteBlockDropEntryData(BlockDropEntryData value) {
        WriteString(value.Identifier);
        _writer.Write(value.MinAmount);
        _writer.Write(value.MaxAmount);
        _writer.Write(value.Chance);
    }

    private void WriteBlockStateData(BlockStateData value) {
        WriteString(value.Identifier);
        WriteString(value.Type);
        WriteList(value.Values, value5 => {
            WriteElement(value5);
        });
    }

    private void WriteItemTypeData(ItemTypeData value) {
        WriteString(value.Identifier);
        WriteList(value.Tags, value6 => {
            WriteString(value6);
        });
        _writer.Write(value.MaxAmount);
        _writer.Write(value.ComponentBased);
        if (value.NetworkId is { } value7) {
            _writer.Write(true);
            _writer.Write(value7);
        }
        else {
            _writer.Write(false);
        }
        _writer.Write(value.ItemVersion);
        if (value.PropertiesPayload is { } value8) {
            _writer.Write(true);
            WriteElement(value8);
        }
        else {
            _writer.Write(false);
        }
        if (value.Catalog is { } value9) {
            _writer.Write(true);
            WriteItemCatalogData(value9);
        }
        else {
            _writer.Write(false);
        }
    }

    private void WriteItemCatalogData(ItemCatalogData value) {
        WriteString(value.CategoryName);
        if (value.GroupIdentifier is { } value10) {
            _writer.Write(true);
            WriteItemGroupIdentifierData(value10);
        }
        else {
            _writer.Write(false);
        }
    }

    private void WriteItemGroupIdentifierData(ItemGroupIdentifierData value) {
        WriteString(value.Icon);
        WriteString(value.Name);
    }

    private void WriteEntityTypeData(EntityTypeData value) {
        WriteString(value.Identifier);
        WriteList(value.Components, value11 => {
            WriteString(value11);
        });
        if (value.Loot is { } value12) {
            _writer.Write(true);
            WriteEntityLootData(value12);
        }
        else {
            _writer.Write(false);
        }
        if (value.PropertiesPayload is { } value13) {
            _writer.Write(true);
            WriteEntityPropertiesPayloadData(value13);
        }
        else {
            _writer.Write(false);
        }
    }

    private void WriteEntityLootData(EntityLootData value) {
        WriteString(value.Table);
    }

    private void WriteEntityPropertiesPayloadData(EntityPropertiesPayloadData value) {
        WriteDictionary(value.Components, value14 => {
            WriteElement(value14);
        });
        WriteDictionary(value.ComponentGroups, value15 => {
            WriteDictionary(value15, value16 => {
                WriteElement(value16);
            });
        });
    }
}

