using System.Text.Json.Serialization;
using BasaltBE.Basalt.Blocks.Types;
using Basalt.Binary;
using BasaltBE.Nbt.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Blocks;

public sealed class BlockPermutation
{
    public int GetCollisionBoxes(Span<BlockCollisionBox> boxes)
    {
        if (boxes.Length < 5)
            throw new ArgumentException("Collision queries require space for five boxes.", nameof(boxes));
        if (Identifier is "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air" or
            "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava")
            return 0;
        BlockType? type = BlockPalette.FindType(Identifier);
        if (type?.CollisionBoxes is { } customBoxes)
        {
            customBoxes.AsSpan().CopyTo(boxes);
            return customBoxes.Length;
        }
        if (State.Values.Count == 0 && Identifier != "minecraft:grass_path")
        {
            if (type?.Solid != true)
                return 0;
            boxes[0] = new(0, 0, 0, 1, 1, 1);
            return 1;
        }
        if (BlockPalette.IsSlab(Identifier))
        {
            bool upper = State.Values.GetValueOrDefault("minecraft:vertical_half") is "top";
            boxes[0] = new(0, upper ? 0.5f : 0, 0, 1, upper ? 1 : 0.5f, 1);
            return 1;
        }
        if (State.Values.GetValueOrDefault("weirdo_direction") is int stair &&
            (uint)stair < 4 && State.Values.GetValueOrDefault("upside_down_bit") is bool inverted)
        {
            boxes[0] = new(0, inverted ? 0.5f : 0, 0, 1, inverted ? 1 : 0.5f, 1);
            int dx = BlockDirections.StairOffsetXByDirection[stair];
            int dz = BlockDirections.StairOffsetZByDirection[stair];
            string corner = State.Values.GetValueOrDefault("minecraft:corner") as string ?? "none";
            int count = 1;
            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 2; z++)
                {
                    int quarterX = x * 2 - 1;
                    int quarterZ = z * 2 - 1;
                    bool front = quarterX * dx + quarterZ * dz > 0;
                    bool left = quarterX * dz - quarterZ * dx > 0;
                    bool raised = front;
                    if (corner == "outer_left") raised = front && left;
                    else if (corner == "outer_right") raised = front && !left;
                    else if (corner == "inner_left") raised = front || left;
                    else if (corner == "inner_right") raised = front || !left;
                    if (raised)
                        boxes[count++] = new(x * 0.5f, inverted ? 0 : 0.5f, z * 0.5f,
                            (x + 1) * 0.5f, inverted ? 0.5f : 1, (z + 1) * 0.5f);
                }
            }
            return count;
        }
        if (Identifier is "minecraft:farmland" or "minecraft:grass_path")
        {
            boxes[0] = new(0, 0, 0, 1, 15f / 16, 1);
            return 1;
        }
        if (Identifier == "minecraft:snow_layer" && State.Values.GetValueOrDefault("height") is int snow)
        {
            if (snow == 0)
                return 0;
            boxes[0] = new(0, 0, 0, 1, snow / 8f, 1);
            return 1;
        }
        if (BlockPalette.IsFenceGate(Identifier))
        {
            if (State.Values.GetValueOrDefault("open_bit") is true)
                return 0;
            string direction = State.Values.GetValueOrDefault("minecraft:cardinal_direction") as string ?? "south";
            bool alongX = BlockDirections.OffsetByCardinalDirection[direction].Z != 0;
            boxes[0] = alongX ? new(0, 0, 0.375f, 1, 1.5f, 0.625f) : new(0.375f, 0, 0, 0.625f, 1.5f, 1);
            return 1;
        }
        if (BlockPalette.IsFence(Identifier) || Identifier.EndsWith("_pane", StringComparison.Ordinal) || Identifier == "minecraft:iron_bars")
        {
            bool fence = BlockPalette.IsFence(Identifier);
            float min = fence ? 0.375f : 0.4375f;
            float max = 1 - min;
            float height = fence ? 1.5f : 1;
            boxes[0] = new(min, 0, min, max, height, max);
            int count = 1;
            foreach ((string state, int dx, int dz) in BlockDirections.Connections)
            {
                if (State.Values.GetValueOrDefault(state) is not true)
                    continue;
                boxes[count++] = new(dx < 0 ? 0 : min, 0, dz < 0 ? 0 : min,
                    dx > 0 ? 1 : max, height, dz > 0 ? 1 : max);
            }
            return count;
        }
        if (BlockPalette.IsTrapdoor(Identifier))
        {
            if (State.Values.GetValueOrDefault("open_bit") is not true)
            {
                bool upper = State.Values.GetValueOrDefault("upside_down_bit") is true;
                boxes[0] = new(0, upper ? 0.8175f : 0, 0, 1, upper ? 1 : 0.1825f, 1);
            }
            else
            {
                int direction = State.Values.GetValueOrDefault("direction") is int facing ? facing : 0;
                (float minX, float minZ, float maxX, float maxZ) = BlockDirections.TrapdoorBounds[direction & 3];
                boxes[0] = new(minX, 0, minZ, maxX, 1, maxZ);
            }
            return 1;
        }
        if (BlockPalette.IsDoor(Identifier))
        {
            int direction = State.Values.GetValueOrDefault("minecraft:cardinal_direction") is string facing
                ? Math.Max(0, Array.IndexOf(BlockDirections.DoorDirection, facing)) : 0;
            if (State.Values.GetValueOrDefault("open_bit") is true)
                direction = (direction + (State.Values.GetValueOrDefault("door_hinge_bit") is true ? 3 : 1)) & 3;
            (float minX, float minZ, float maxX, float maxZ) = BlockDirections.DoorBounds[direction];
            boxes[0] = new(minX, 0, minZ, maxX, 1, maxZ);
            return 1;
        }
        if (type?.Solid != true)
            return 0;
        boxes[0] = new(0, 0, 0, 1, 1, 1);
        return 1;
    }

    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("hash")]
    public int Hash;

    [JsonIgnore]
    internal int NetworkId;

    [JsonInclude, JsonPropertyName("state")]
    public BlockState State = new();

    internal static int CalculateHash(string identifier, BlockState state)
    {
        CompoundTag values = new();
        foreach ((string name, object value) in state.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            values.Add(name, value switch
            {
                bool bit => new ByteTag { Value = bit ? (byte)1 : (byte)0 },
                int number => new IntTag { Value = number },
                string text => new StringTag { Value = text },
                _ => throw new ArgumentException("Block states must contain bool, int or string values.", nameof(state))
            });

        CompoundTag tag = new CompoundTag().Add("name", new StringTag { Value = identifier }).Add("states", values);

        using BinaryStream stream = BinaryStream.Rent(tag.GetSize());
        BinaryWriter writer = stream.GetWriter();

        BasaltBE.Nbt.Nbt.Write(tag, ref writer);

        uint hash = 2166136261;
        foreach (byte value in stream.Buffer.Span[..stream.Offset])
            hash = unchecked((hash ^ value) * 16777619);
        return unchecked((int)hash);
    }
}
