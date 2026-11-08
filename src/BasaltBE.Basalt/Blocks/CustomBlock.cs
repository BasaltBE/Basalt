using Basalt.Binary;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Blocks;

public sealed class CustomBlock
{
    public readonly BlockType Type;
    public readonly CustomItem Item;
    internal readonly Dictionary<string, object[]> States = new(StringComparer.Ordinal);
    private readonly CompoundTag components = new();
    private readonly BlockPermutation defaults;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;

    internal CustomBlock(BlockType type, BlockPermutation permutation, CustomItem item)
    {
        Type = type;
        defaults = permutation;
        Item = item.SetPlacedBlock(type.Identifier)
            .SetCreativeCategory(CreativeItemCategory.Construction);
        SetGeometry("minecraft:geometry.full_block");
        SetHardness(type.Hardness);
        SetFriction(type.Friction);
        SetCollisionBox(new BlockCollisionBox(0, 0, 0, 1, 1, 1));
        SetSelectionBox(new(0, 0, 0, 1, 1, 1));
    }

    public CustomBlock SetDisplayName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Item.SetDisplayName(name);
        return SetComponent("minecraft:display_name", new CompoundTag().Add("value", new StringTag { Value = name }));
    }

    public CustomBlock SetHardness(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value > float.MaxValue / 1.5f)
            throw new ArgumentOutOfRangeException(nameof(value));
        Mutable();
        Type.Hardness = value;
        return SetComponent("minecraft:destructible_by_mining", new CompoundTag().Add("value", new FloatTag { Value = value * 1.5f }));
    }

    public CustomBlock SetBlastResistance(float value)
    {
        if (!float.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        Mutable();
        Type.BlastResistance = value;
        return this;
    }

    public CustomBlock SetFriction(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value));
        Mutable();
        Type.Friction = value;
        return SetComponent("minecraft:friction", new CompoundTag().Add("value", new FloatTag { Value = value }));
    }

    public CustomBlock SetLightEmission(int value)
    {
        if (value is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(value));
        Mutable();
        Type.Brightness = value;
        return SetComponent("minecraft:light_emission", new CompoundTag().Add("emission", new ByteTag { Value = (byte)value }));
    }

    public CustomBlock SetLightDampening(int value)
    {
        if (value is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(value));
        Mutable();
        Type.Opacity = value / 15f;
        return SetComponent("minecraft:light_dampening", new CompoundTag().Add("lightLevel", new ByteTag { Value = (byte)value }));
    }

    public CustomBlock SetGeometry(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return SetComponent("minecraft:geometry", new CompoundTag()
            .Add("identifier", new StringTag { Value = identifier })
            .Add("bone_visibility", new CompoundTag())
            .Add("culling", new StringTag { Value = string.Empty })
            .Add("culling_layer", new StringTag { Value = "minecraft:culling_layer.undefined" })
            .Add("culling_shape", new StringTag { Value = string.Empty })
            .Add("ignoreGeometryForIsSolid", new ByteTag())
            .Add("isV1Fullblock", new ByteTag())
            .Add("needsLegacyTopRotation", new ByteTag())
            .Add("useBlockTypeLightAbsorption", new ByteTag())
            .Add("uv_lock", new ByteTag()));
    }

    public CustomBlock SetMaterial(string texture, string renderMethod = "opaque", string face = "*")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texture);
        ArgumentException.ThrowIfNullOrWhiteSpace(face);
        if (renderMethod is not ("opaque" or "alpha_test" or "blend" or "double_sided"))
            throw new ArgumentException("Use a supported block render method.", nameof(renderMethod));
        Mutable();
        CompoundTag materials = components.Get<CompoundTag>("minecraft:material_instances") ?? new CompoundTag()
            .Add("mappings", new CompoundTag()).Add("materials", new CompoundTag());
        materials.Get<CompoundTag>("materials")!.Add(face, new CompoundTag()
            .Add("texture", new StringTag { Value = texture })
            .Add("render_method", new StringTag { Value = renderMethod })
            .Add("ambient_occlusion", new FloatTag { Value = 1 })
            .Add("packed_bools", new ByteTag { Value = 1 })
            .Add("tint_method", new StringTag { Value = "none" }));
        components.Add("minecraft:material_instances", materials);
        return this;
    }

    public CustomBlock SetCollisionBox(params BlockCollisionBox[] boxes)
    {
        ArgumentNullException.ThrowIfNull(boxes);
        if (boxes.Length > 5)
            throw new ArgumentException("Use at most five collision boxes.", nameof(boxes));
        foreach (BlockCollisionBox box in boxes)
            ValidateBox(box);
        Mutable();
        Type.CollisionBoxes = (BlockCollisionBox[])boxes.Clone();
        Type.Solid = boxes.Length > 0;
        ListTag entries = new() { ElementType = TagType.Compound };
        foreach (BlockCollisionBox box in boxes)
            entries.Value.Add(new CompoundTag()
                .Add("minX", new FloatTag { Value = box.MinX * 16 }).Add("minY", new FloatTag { Value = box.MinY * 16 })
                .Add("minZ", new FloatTag { Value = box.MinZ * 16 }).Add("maxX", new FloatTag { Value = box.MaxX * 16 })
                .Add("maxY", new FloatTag { Value = box.MaxY * 16 }).Add("maxZ", new FloatTag { Value = box.MaxZ * 16 }));
        return SetComponent("minecraft:collision_box", new CompoundTag()
            .Add("enabled", new ByteTag { Value = boxes.Length > 0 ? (byte)1 : (byte)0 }).Add("boxes", entries));
    }

    public CustomBlock SetSelectionBox(BlockCollisionBox box)
    {
        ValidateBox(box);
        return SetComponent("minecraft:selection_box", new CompoundTag().Add("enabled", new ByteTag { Value = 1 })
            .Add("origin", new ListTag
            {
                ElementType = TagType.Float,
                Value = [
                new FloatTag { Value = box.MinX * 16 - 8 }, new FloatTag { Value = box.MinY * 16 }, new FloatTag { Value = box.MinZ * 16 - 8 }]
            })
            .Add("size", new ListTag
            {
                ElementType = TagType.Float,
                Value = [
                new FloatTag { Value = (box.MaxX - box.MinX) * 16 }, new FloatTag { Value = (box.MaxY - box.MinY) * 16 }, new FloatTag { Value = (box.MaxZ - box.MinZ) * 16 }]
            }));
    }

    public CustomBlock SetTags(params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (tags.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Use non-empty block tags.", nameof(tags));
        Mutable();
        Type.Tags = tags.Distinct(StringComparer.Ordinal).ToArray();
        Type.ToolTypes = ToolType.None;
        if (tags.Contains("minecraft:is_pickaxe_item_destructible")) Type.ToolTypes |= ToolType.Pickaxe;
        if (tags.Contains("minecraft:is_axe_item_destructible")) Type.ToolTypes |= ToolType.Axe;
        if (tags.Contains("minecraft:is_shovel_item_destructible")) Type.ToolTypes |= ToolType.Shovel;
        if (tags.Contains("minecraft:is_hoe_item_destructible")) Type.ToolTypes |= ToolType.Hoe;
        return this;
    }

    public CustomBlock AddTrait<TTrait>() where TTrait : BlockTrait, new()
    {
        Mutable();
        BlockPalette.RegisterCustomTrait<TTrait>(Type.Identifier);
        return this;
    }

    public CustomBlock AddState(string identifier, params bool[] values) => AddState(identifier, values.Cast<object>().ToArray());
    public CustomBlock AddState(string identifier, params int[] values) => AddState(identifier, values.Cast<object>().ToArray());
    public CustomBlock AddState(string identifier, params string[] values) => AddState(identifier, values.Cast<object>().ToArray());

    private CustomBlock AddState(string identifier, object[] values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (identifier.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not (':' or '_' or '-' or '.' or '/')) ||
            values.Length is < 1 or > 16 || values.Any(value => value is null || value is string text && string.IsNullOrWhiteSpace(text)))
            throw new ArgumentException("Use a state name and 1 to 16 non-empty values.", nameof(values));
        Mutable();

        if (States.ContainsKey(identifier))
            throw new InvalidOperationException($"State '{identifier}' is already defined.");
        object[] uniqueValues = values.Distinct().ToArray();
        int count = uniqueValues.Length;

        foreach (object[] existing in States.Values)
            count = checked(count * existing.Length);
        if (count > 65536)
            throw new ArgumentException("A block can have at most 65536 permutations.", nameof(values));

        States.Add(identifier, uniqueValues);
        Type.States = States.Keys.ToArray();
        return this;
    }

    public CustomBlock SetComponent(string identifier, CompoundTag data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(data);
        Mutable();

        using BinaryStream stream = BinaryStream.Rent(data.GetSize());
        BinaryWriter writer = stream.GetWriter();

        BasaltBE.Nbt.Nbt.Write(data, ref writer);

        int offset = 0;
        components.Add(identifier, BasaltBE.Nbt.Nbt.Read(stream.Buffer.Span[..stream.Offset], ref offset));
        return this;
    }

    internal BlockPermutation[] Prepare(int blockId, out ServerBlockProperty property)
    {
        if (components.Get<CompoundTag>("minecraft:material_instances")?.Get<CompoundTag>("materials")?.Tags.Count is not > 0)
            throw new InvalidOperationException($"Custom block '{Type.Identifier}' requires a material.");
        List<BlockState> permutations = [new()];
        ListTag properties = new() { ElementType = TagType.Compound };

        foreach ((string name, object[] values) in States.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            List<BlockState> expanded = new(permutations.Count * values.Length);
            foreach (BlockState state in permutations)
            {
                foreach (object value in values)
                {
                    BlockState next = new() { Values = new(state.Values, StringComparer.Ordinal) };
                    next.Values.Add(name, value);
                    expanded.Add(next);
                }
            }
            permutations = expanded;

            ListTag entries = new();
            foreach (object value in values)
                entries.Value.Add(value switch
                {
                    bool bit => new ByteTag { Value = bit ? (byte)1 : (byte)0 },
                    int number => new IntTag { Value = number },
                    string text => new StringTag { Value = text },
                    _ => throw new InvalidOperationException("Unsupported custom state type.")
                });
            properties.Value.Add(new CompoundTag().Add("name", new StringTag { Value = name }).Add("enum", entries));
        }
        BlockPermutation[] result = new BlockPermutation[permutations.Count];
        for (int i = 0; i < result.Length; i++)
        {
            BlockPermutation permutation = i == 0 ? defaults : new() { Identifier = Type.Identifier };
            permutation.State = permutations[i];
            permutation.Hash = permutation.NetworkId = BlockPermutation.CalculateHash(Type.Identifier, permutation.State);
            result[i] = permutation;
        }
        ListTag tags = new() { ElementType = TagType.String };
        foreach (string tag in Type.Tags)
            tags.Value.Add(new StringTag { Value = tag });
        CompoundTag definition = new CompoundTag()
            .Add("components", components).Add("molangVersion", new IntTag { Value = 13 })
            .Add("vanilla_block_data", new CompoundTag().Add("block_id", new IntTag { Value = blockId }).Add("material", new StringTag { Value = "dirt" }))
            .Add("menu_category", new CompoundTag().Add("category", new StringTag { Value = Item.Type.Catalog?.CategoryName ?? "none" })
                .Add("group", new StringTag { Value = Item.Type.Catalog?.GroupIdentifier?.Name ?? string.Empty }).Add("is_hidden_in_commands", new ByteTag()))
            .Add("properties", properties).Add("blockTags", tags);
        property = new ServerBlockProperty { BlockName = Type.Identifier, BlockDefinition = definition };
        return result;
    }

    private static void ValidateBox(BlockCollisionBox box)
    {
        if (!float.IsFinite(box.MinX) || !float.IsFinite(box.MinY) || !float.IsFinite(box.MinZ) ||
            !float.IsFinite(box.MaxX) || !float.IsFinite(box.MaxY) || !float.IsFinite(box.MaxZ) ||
            box.MinX < 0 || box.MinY < 0 || box.MinZ < 0 || box.MaxX > 1 || box.MaxY > 1 || box.MaxZ > 1 ||
            box.MinX >= box.MaxX || box.MinY >= box.MaxY || box.MinZ >= box.MaxZ)
            throw new ArgumentException("Block boxes must have positive size within 0 to 1.", nameof(box));
    }

    private void Mutable()
    {
        if (BlockPalette.Frozen || Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Configure blocks on their creating thread before starting the server.");
    }
}
