namespace Basalt.Core.Blocks;

using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Types;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Types;

/// <summary>
/// Options for defining a custom block type.
/// </summary>
public sealed class CustomBlockTypeOptions
{
    /// <summary>
    /// The namespaced identifier, such as "mynamespace:ruby_block".
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// The resource-pack texture key used on every face when Materials is empty.
    /// </summary>
    public string? Texture { get; init; }

    /// <summary>
    /// The material render method: opaque, alpha_test, blend, or double_sided.
    /// </summary>
    public string RenderMethod { get; init; } = "opaque";

    public IReadOnlyDictionary<string, CustomBlockMaterial>? Materials { get; init; }
    public string? Geometry { get; init; }
    public string? DisplayName { get; init; }
    public CustomBlockBox? SelectionBox { get; init; }
    public IReadOnlyList<CustomBlockBox>? CollisionBoxes { get; init; }

    /// <summary>
    /// Registered block states. An empty list creates one state-less permutation.
    /// </summary>
    public IReadOnlyList<CustomBlockPermutationOptions>? Permutations { get; init; }

    public IReadOnlyList<string>? Tags { get; init; }
    public bool Solid { get; init; } = true;
    public float Hardness { get; init; } = 1f;
    public float BlastResistance { get; init; } = 1f;
    public float Friction { get; init; } = 0.6f;
    public int LightEmission { get; init; }
    public int LightFilter { get; init; } = 15;
    public int FlameEncouragement { get; init; }
    public int Flammability { get; init; }
    public string? MapColor { get; init; }
}

/// <summary>
/// Factory for creating and registering custom block types.
/// </summary>
public static class CustomBlockType
{
    private static readonly List<BlockEntry> Entries = [];
    private static int _nextBlockId = 10000;

    /// <summary>
    /// Creates and registers a custom block and its permutations.
    /// </summary>
    public static BlockType Create(CustomBlockTypeOptions options)
    {
        BlockType type = new(options.Identifier)
        {
            Solid = options.Solid,
            Hardness = options.Hardness,
            BlastResistance = options.BlastResistance,
            Friction = options.Friction,
            Brightness = Math.Clamp(options.LightEmission, 0, 15),
            Opacity = Math.Clamp(options.LightFilter, 0, 15),
            FlameEncouragement = options.FlameEncouragement,
            Flammability = options.Flammability,
            MapColor = options.MapColor
        };

        if (options.Tags is not null)
        {
            for (int i = 0; i < options.Tags.Count; i++)
            {
                type.EnsureTag(options.Tags[i]);
            }

            BlockTraitRegistry.BindTraitsToType(type);
        }

        IReadOnlyList<CustomBlockPermutationOptions> permutations = options.Permutations is { Count: > 0 }
            ? options.Permutations
            : [new CustomBlockPermutationOptions { State = new BlockState() }];

        List<BlockPermutation> registeredPermutations = new(permutations.Count);

        for (int i = 0; i < permutations.Count; i++)
        {
            registeredPermutations.Add(BlockPermutation.Create(type, permutations[i].State));
        }

        Entries.Add(new BlockEntry
        {
            Name = options.Identifier,
            Properties = BuildProperties(options, permutations, registeredPermutations, _nextBlockId++)
        });

        return type;
    }

    internal static List<BlockEntry> GetEntries()
    {
        return [.. Entries];
    }

    private static CompoundTag BuildProperties(
        CustomBlockTypeOptions options,
        IReadOnlyList<CustomBlockPermutationOptions> permutations,
        IReadOnlyList<BlockPermutation> registeredPermutations,
        int blockId)
    {
        CompoundTag properties = new();
        CompoundTag components = BuildComponents(options);
        properties.Set("molangVersion", new IntTag { Value = 10 });

        CompoundTag menuCategory = new();
        menuCategory.Set("category", new StringTag { Value = "construction" });
        menuCategory.Set("group", new StringTag { Value = string.Empty });
        properties.Set("menu_category", menuCategory);

        CompoundTag vanillaBlockData = new();
        vanillaBlockData.Set("block_id", new IntTag { Value = blockId });
        properties.Set("vanilla_block_data", vanillaBlockData);

        ListTag stateProperties = BuildStateProperties(permutations);
        if (stateProperties.Values.Count > 0)
        {
            properties.Set("properties", stateProperties);
        }

        ListTag permutationEntries = BuildPermutationEntries(permutations, registeredPermutations);
        if (permutationEntries.Values.Count > 0)
        {
            CompoundTag onPlayerPlacing = new();
            onPlayerPlacing.Set("triggerType", new StringTag { Value = "placement_trigger" });
            components.Set("minecraft:on_player_placing", onPlayerPlacing);
            properties.Set("permutations", permutationEntries);
        }

        properties.Set("components", components);

        return properties;
    }

    private static CompoundTag BuildComponents(CustomBlockTypeOptions options)
    {
        CompoundTag components = new();

        if (string.IsNullOrEmpty(options.Geometry))
        {
            components.Set("minecraft:unit_cube", new CompoundTag());
        }
        else
        {
            CompoundTag geometry = new();
            geometry.Set("identifier", new StringTag { Value = options.Geometry });
            geometry.Set("bone_visibility", new CompoundTag());
            components.Set("minecraft:geometry", geometry);
        }

        CompoundTag materials = new();
        if (options.Materials is { Count: > 0 })
        {
            foreach ((string face, CustomBlockMaterial material) in options.Materials)
            {
                materials.Set(face, BuildMaterial(material));
            }
        }
        else if (!string.IsNullOrEmpty(options.Texture))
        {
            materials.Set("*", BuildMaterial(new CustomBlockMaterial
            {
                Texture = options.Texture,
                RenderMethod = options.RenderMethod,
                AmbientOcclusion = options.RenderMethod is not ("alpha_test" or "blend")
            }));
        }
        else
        {
            throw new InvalidOperationException($"Custom block '{options.Identifier}' requires Texture or Materials.");
        }

        CompoundTag materialInstances = new();
        materialInstances.Set("mappings", new CompoundTag());
        materialInstances.Set("materials", materials);
        components.Set("minecraft:material_instances", materialInstances);

        if (options.CollisionBoxes is { Count: > 0 })
        {
            components.Set("minecraft:collision_box", BuildCollisionBox(options.CollisionBoxes));
        }
        else if (!options.Solid)
        {
            CompoundTag collisionBox = new();
            collisionBox.Set("enabled", new ByteTag { Value = 0 });
            components.Set("minecraft:collision_box", collisionBox);
        }

        if (options.SelectionBox is CustomBlockBox selection)
        {
            components.Set("minecraft:selection_box", BuildSelectionBox(selection));
        }

        if (!string.IsNullOrEmpty(options.DisplayName))
        {
            CompoundTag displayName = new();
            displayName.Set("value", new StringTag { Value = options.DisplayName });
            components.Set("minecraft:display_name", displayName);
        }

        CompoundTag mining = new();
        mining.Set("value", new FloatTag { Value = options.Hardness });
        components.Set("minecraft:destructible_by_mining", mining);

        CompoundTag friction = new();
        friction.Set("value", new FloatTag { Value = options.Friction });
        components.Set("minecraft:friction", friction);

        if (options.LightEmission > 0)
        {
            CompoundTag emission = new();
            emission.Set("emission", new FloatTag { Value = Math.Clamp(options.LightEmission, 0, 15) / 15f });
            components.Set("minecraft:block_light_emission", emission);
        }

        if (options.LightFilter != 15)
        {
            CompoundTag filter = new();
            filter.Set("lightLevel", new IntTag { Value = Math.Clamp(options.LightFilter, 0, 15) });
            components.Set("minecraft:block_light_filter", filter);
        }

        if (options.FlameEncouragement > 0 || options.Flammability > 0)
        {
            CompoundTag flammable = new();
            flammable.Set("flame_odds", new IntTag { Value = options.FlameEncouragement });
            flammable.Set("burn_odds", new IntTag { Value = options.Flammability });
            components.Set("minecraft:flammable", flammable);
        }

        if (!string.IsNullOrEmpty(options.MapColor))
        {
            CompoundTag mapColor = new();
            mapColor.Set("value", new StringTag { Value = options.MapColor });
            components.Set("minecraft:map_color", mapColor);
        }

        return components;
    }

    private static CompoundTag BuildMaterial(CustomBlockMaterial options)
    {
        CompoundTag material = new();
        material.Set("texture", new StringTag { Value = options.Texture });
        material.Set("render_method", new StringTag { Value = options.RenderMethod });
        material.Set("face_dimming", new ByteTag { Value = options.FaceDimming ? (sbyte)1 : (sbyte)0 });
        material.Set("ambient_occlusion", new ByteTag { Value = options.AmbientOcclusion ? (sbyte)1 : (sbyte)0 });
        return material;
    }

    private static ListTag BuildFloatList(float x, float y, float z)
    {
        ListTag values = new();
        values.Values.Add(new FloatTag { Value = x });
        values.Values.Add(new FloatTag { Value = y });
        values.Values.Add(new FloatTag { Value = z });
        return values;
    }

    private static ListTag BuildStateProperties(IReadOnlyList<CustomBlockPermutationOptions> permutations)
    {
        Dictionary<string, List<BlockStateValue>> values = new(StringComparer.Ordinal);
        for (int i = 0; i < permutations.Count; i++)
        {
            foreach ((string name, BlockStateValue value) in permutations[i].State)
            {
                if (!values.TryGetValue(name, out List<BlockStateValue>? stateValues))
                {
                    stateValues = [];
                    values[name] = stateValues;
                }

                if (!stateValues.Contains(value))
                {
                    stateValues.Add(value);
                }
            }
        }

        ListTag properties = new();
        foreach ((string name, List<BlockStateValue> stateValues) in values)
        {
            ListTag entries = new();
            for (int i = 0; i < stateValues.Count; i++)
            {
                if (i > 0 && stateValues[i].Kind != stateValues[0].Kind)
                {
                    throw new InvalidOperationException($"Block state '{name}' cannot mix value types.");
                }

                entries.Values.Add(ToTag(stateValues[i]));
            }

            CompoundTag property = new();
            property.Set("name", new StringTag { Value = name });
            property.Set("enum", entries);
            properties.Values.Add(property);
        }

        return properties;
    }

    private static ListTag BuildPermutationEntries(
        IReadOnlyList<CustomBlockPermutationOptions> permutations,
        IReadOnlyList<BlockPermutation> registeredPermutations)
    {
        ListTag entries = new();
        for (int i = 0; i < permutations.Count; i++)
        {
            CustomBlockPermutationOptions options = permutations[i];
            CompoundTag components = new();

            if (options.Transformation is CustomBlockTransformation transformation)
            {
                CompoundTag value = new();
                value.Set("TX", new FloatTag { Value = 0 });
                value.Set("TY", new FloatTag { Value = 0 });
                value.Set("TZ", new FloatTag { Value = 0 });
                value.Set("RX", new IntTag { Value = ToQuarterTurns(transformation.RotationX) });
                value.Set("RY", new IntTag { Value = ToQuarterTurns(transformation.RotationY) });
                value.Set("RZ", new IntTag { Value = ToQuarterTurns(transformation.RotationZ) });
                value.Set("SX", new FloatTag { Value = 1 });
                value.Set("SY", new FloatTag { Value = 1 });
                value.Set("SZ", new FloatTag { Value = 1 });
                value.Set("RXP", new FloatTag { Value = 0 });
                value.Set("RYP", new FloatTag { Value = 0 });
                value.Set("RZP", new FloatTag { Value = 0 });
                value.Set("SXP", new FloatTag { Value = 0 });
                value.Set("SYP", new FloatTag { Value = 0 });
                value.Set("SZP", new FloatTag { Value = 0 });
                components.Set("minecraft:transformation", value);
            }

            if (options.CollisionBoxes is { Count: > 0 })
            {
                components.Set("minecraft:collision_box", BuildCollisionBox(options.CollisionBoxes));
            }

            if (options.SelectionBox is CustomBlockBox selectionBox)
            {
                components.Set("minecraft:selection_box", BuildSelectionBox(selectionBox));
            }

            if (components.Values.Count == 0)
            {
                continue;
            }

            CompoundTag entry = new();
            entry.Set("condition", new StringTag { Value = registeredPermutations[i].Query });
            entry.Set("components", components);
            entries.Values.Add(entry);
        }

        return entries;
    }

    private static CompoundTag BuildCollisionBox(IReadOnlyList<CustomBlockBox> boxes)
    {
        CompoundTag collisionBox = new();
        collisionBox.Set("enabled", new ByteTag { Value = 1 });
        ListTag values = new();
        for (int i = 0; i < boxes.Count; i++)
        {
            CustomBlockBox box = boxes[i];
            CompoundTag entry = new();
            entry.Set("minX", new FloatTag { Value = box.X });
            entry.Set("minY", new FloatTag { Value = box.Y });
            entry.Set("minZ", new FloatTag { Value = box.Z });
            entry.Set("maxX", new FloatTag { Value = box.Width });
            entry.Set("maxY", new FloatTag { Value = box.Height });
            entry.Set("maxZ", new FloatTag { Value = box.Depth });
            values.Values.Add(entry);
        }
        collisionBox.Set("boxes", values);
        return collisionBox;
    }

    private static CompoundTag BuildSelectionBox(CustomBlockBox box)
    {
        CompoundTag selectionBox = new();
        selectionBox.Set("enabled", new ByteTag { Value = 1 });
        selectionBox.Set("origin", BuildFloatList(box.X, box.Y, box.Z));
        selectionBox.Set("size", BuildFloatList(box.Width, box.Height, box.Depth));
        return selectionBox;
    }

    private static int ToQuarterTurns(int degrees)
    {
        if (degrees % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degrees), "Custom block rotations must use 90 degree increments.");
        }

        return degrees / 90 % 4;
    }

    private static BaseTag ToTag(BlockStateValue value)
    {
        return value.Kind switch
        {
            0 => new IntTag { Value = checked((int)value.AsNumber()) },
            1 => new StringTag { Value = value.AsString() },
            2 => new ByteTag { Value = value.AsBool() ? (sbyte)1 : (sbyte)0 },
            _ => throw new InvalidOperationException("Unsupported block state value kind.")
        };
    }
}
