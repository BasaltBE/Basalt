using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;
using System.Text.Json;
using System.Text.Json.Nodes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Block;

public sealed class BlockLevelStorage : CompoundTag
{
    private readonly ChunkColumn? _chunk;

    public BlockLevelStorage(ChunkColumn? chunk = null, CompoundTag? source = null)
    {
        _chunk = chunk;
        if (source is null)
        {
            return;
        }

        foreach ((string key, BaseTag value) in source.Values)
        {
            Values[key] = value;
        }
    }

    public int Size
    {
        get
        {
            int size = Values.Count;
            if (Values.ContainsKey("x")) size--;
            if (Values.ContainsKey("y")) size--;
            if (Values.ContainsKey("z")) size--;
            return size;
        }
    }

    public new BlockLevelStorage Set(string key, BaseTag value)
    {
        base.Set(key, value);
        MarkDirtyIfNeeded();
        return this;
    }

    public T Add<T>(T tag) where T : BaseTag
    {
        if (string.IsNullOrEmpty(tag.Name))
        {
            throw new InvalidOperationException("Tag must have a name to be added to block level storage.");
        }

        base.Set(tag.Name, tag);
        MarkDirtyIfNeeded();
        return tag;
    }

    public BlockLevelStorage Push(params BaseTag[] tags)
    {
        for (int i = 0; i < tags.Length; i++)
        {
            Add(tags[i]);
        }

        return this;
    }

    public bool Delete(string key)
    {
        bool removed = Values.Remove(key);
        if (removed)
        {
            MarkDirtyIfNeeded();
        }

        return removed;
    }

    public void ClearStorage()
    {
        List<string> keys = [.. Values.Keys];
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (key == "x" || key == "y" || key == "z")
            {
                continue;
            }

            Values.Remove(key);
        }
    }

    public BlockPos GetPosition()
    {
        int x = Get<IntTag>("x")?.Value ?? 0;
        int y = Get<IntTag>("y")?.Value ?? 0;
        int z = Get<IntTag>("z")?.Value ?? 0;

        return new BlockPos { X = x, Y = y, Z = z };
    }

    public void SetPosition(BlockPos position)
    {
        Set("x", new IntTag { Name = "x", Value = position.X });
        Set("y", new IntTag { Name = "y", Value = position.Y });
        Set("z", new IntTag { Name = "z", Value = position.Z });
    }

    public List<(string Identifier, JsonNode? Value)> GetDynamicProperties()
    {
        List<(string Identifier, JsonNode? Value)> properties = [];
        ListTag? dynamicProperties = Get<ListTag>("dynamic_properties");
        if (dynamicProperties is null)
        {
            return properties;
        }

        for (int i = 0; i < dynamicProperties.Values.Count; i++)
        {
            if (dynamicProperties.Values[i] is not CompoundTag property)
            {
                continue;
            }

            StringTag? identifier = property.Get<StringTag>("identifier");
            StringTag? value = property.Get<StringTag>("value");
            if (identifier is null || value is null)
            {
                continue;
            }

            properties.Add((identifier.Value, JsonNode.Parse(value.Value)));
        }

        return properties;
    }

    public void SetDynamicProperties(IEnumerable<(string Identifier, JsonNode? Value)> properties)
    {
        ListTag dynamicProperties = new() { Name = "dynamic_properties" };
        foreach ((string identifier, JsonNode? value) in properties)
        {
            CompoundTag propertyTag = new();
            propertyTag.Set("identifier", new StringTag { Name = "identifier", Value = identifier });
            propertyTag.Set("value", new StringTag { Name = "value", Value = value?.ToJsonString() ?? "null" });
            dynamicProperties.Values.Add(propertyTag);
        }

        Set("dynamic_properties", dynamicProperties);
    }

    public bool HasDynamicProperty(string key)
    {
        ListTag? dynamicProperties = Get<ListTag>("dynamic_properties");
        if (dynamicProperties is null)
        {
            return false;
        }

        for (int i = 0; i < dynamicProperties.Values.Count; i++)
        {
            if (dynamicProperties.Values[i] is not CompoundTag property)
            {
                continue;
            }

            StringTag? identifier = property.Get<StringTag>("identifier");
            if (identifier is not null && identifier.Value == key)
            {
                return true;
            }
        }

        return false;
    }

    public T? GetDynamicProperty<T>(string key)
    {
        ListTag? dynamicProperties = Get<ListTag>("dynamic_properties");
        if (dynamicProperties is null)
        {
            return default;
        }

        for (int i = 0; i < dynamicProperties.Values.Count; i++)
        {
            if (dynamicProperties.Values[i] is not CompoundTag property)
            {
                continue;
            }

            StringTag? identifier = property.Get<StringTag>("identifier");
            StringTag? value = property.Get<StringTag>("value");
            if (identifier is null || value is null || identifier.Value != key)
            {
                continue;
            }

            return JsonSerializer.Deserialize<T>(value.Value);
        }

        return default;
    }

    public void AddDynamicProperty(string key, JsonNode? value)
    {
        ListTag dynamicProperties = EnsureDynamicPropertiesList();

        CompoundTag propertyTag = new();
        propertyTag.Set("identifier", new StringTag { Name = "identifier", Value = key });
        propertyTag.Set("value", new StringTag { Name = "value", Value = value?.ToJsonString() ?? "null" });
        dynamicProperties.Values.Add(propertyTag);

        MarkDirtyIfNeeded();
    }

    public void RemoveDynamicProperty(string key)
    {
        ListTag? dynamicProperties = Get<ListTag>("dynamic_properties");
        if (dynamicProperties is null)
        {
            return;
        }

        for (int i = 0; i < dynamicProperties.Values.Count; i++)
        {
            if (dynamicProperties.Values[i] is not CompoundTag property)
            {
                continue;
            }

            StringTag? identifier = property.Get<StringTag>("identifier");
            if (identifier is null || identifier.Value != key)
            {
                continue;
            }

            dynamicProperties.Values.RemoveAt(i);
            MarkDirtyIfNeeded();
            return;
        }
    }

    public void SetDynamicProperty(string key, JsonNode? value)
    {
        RemoveDynamicProperty(key);
        AddDynamicProperty(key, value);
    }

    public static BlockLevelStorage FromStream(ChunkColumn? chunk, BinaryReader reader)
    {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        CompoundTag tag = Read(reader);
        return new BlockLevelStorage(chunk, tag);
    }

    public static ReadOnlySpan<byte> Write(BlockLevelStorage storage, BinaryWriter writer)
    {
        Protocol.Io.NBT.WriteTag(writer, storage, new TagOptions(Name: true, Type: true, VarInt: false));
        return writer.GetProcessedBytes();
    }

    private ListTag EnsureDynamicPropertiesList()
    {
        ListTag? dynamicProperties = Get<ListTag>("dynamic_properties");
        if (dynamicProperties is not null)
        {
            return dynamicProperties;
        }

        ListTag list = new() { Name = "dynamic_properties" };
        Set("dynamic_properties", list);
        return list;
    }

    private void MarkDirtyIfNeeded()
    {
        if (_chunk is not null && Size > 0)
        {
            _chunk.Dirty = true;
        }
    }
}
