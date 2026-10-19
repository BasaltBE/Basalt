using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Blocks.Traits;

public sealed class StairShapeTrait : BlockTrait {
    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (!current.State.Values.TryGetValue("weirdo_direction", out object? directionValue) ||
            directionValue is not int direction ||
            !current.State.Values.TryGetValue("upside_down_bit", out object? upsideDownValue) ||
            upsideDownValue is not bool upsideDown)
            return false;

        string corner = FindCorner(dimension, x, y, z, direction, upsideDown);
        if (current.State.Values.TryGetValue("minecraft:corner", out object? cornerValue) &&
            cornerValue is string currentCorner && currentCorner == corner)
            return false;

        BlockPermutation? updated = BlockPalette.Find(current.Identifier, current.State, "minecraft:corner", corner);
        if (updated is null)
            return false;

        dimension.SetPermutation(x, y, z, updated);
        return true;
    }

    static string FindCorner(Dimension dimension, int x, int y, int z, int direction, bool upsideDown) {
        int frontX = x + OffsetX(direction);
        int frontZ = z + OffsetZ(direction);
        if (StairTurn(dimension, frontX, y, frontZ, upsideDown, direction, out int frontDirection) &&
            !StairShapeBlocked(dimension, x, y, z, direction, frontDirection, upsideDown, outer: true)) {
            int turn = (StairFacing(frontDirection) - StairFacing(direction) + 4) & 3;
            return turn == 3 ? "outer_left" : "outer_right";
        }

        int backDirection = Opposite(direction);
        int backX = x + OffsetX(backDirection);
        int backZ = z + OffsetZ(backDirection);
        if (StairTurn(dimension, backX, y, backZ, upsideDown, direction, out int rearDirection) &&
            !StairShapeBlocked(dimension, x, y, z, direction, rearDirection, upsideDown, outer: false)) {
            int turn = (StairFacing(rearDirection) - StairFacing(direction) + 4) & 3;
            return turn == 3 ? "inner_left" : "inner_right";
        }

        return "none";
    }

    static bool StairShapeBlocked(Dimension dimension, int x, int y, int z, int direction, int neighborDirection, bool upsideDown, bool outer) {
        int checkDirection = outer ? Opposite(neighborDirection) : neighborDirection;
        int checkX = x + OffsetX(checkDirection);
        int checkZ = z + OffsetZ(checkDirection);
        if (dimension.GetChunk(checkX >> 4, checkZ >> 4) is null)
            return false;

        BlockPermutation block = dimension.GetPermutation(checkX, y, checkZ);
        return BlockPalette.IsCornerableStairs(block.Identifier) &&
            block.State.Values.TryGetValue("weirdo_direction", out object? foundDirection) &&
            foundDirection is int value && value == direction &&
            block.State.Values.TryGetValue("upside_down_bit", out object? foundHalf) &&
            foundHalf is bool valueHalf && valueHalf == upsideDown;
    }

    static bool StairTurn(Dimension dimension, int x, int y, int z, bool upsideDown, int direction, out int neighborDirection) {
        neighborDirection = 0;
        if (dimension.GetChunk(x >> 4, z >> 4) is null)
            return false;

        BlockPermutation neighbor = dimension.GetPermutation(x, y, z);
        if (!BlockPalette.IsCornerableStairs(neighbor.Identifier) ||
            !neighbor.State.Values.TryGetValue("weirdo_direction", out object? directionValue) ||
            directionValue is not int foundDirection ||
            !neighbor.State.Values.TryGetValue("upside_down_bit", out object? upsideDownValue) ||
            upsideDownValue is not bool foundUpsideDown || foundUpsideDown != upsideDown)
            return false;

        int turn = (StairFacing(foundDirection) - StairFacing(direction) + 4) & 3;
        if (turn is not (1 or 3))
            return false;

        neighborDirection = foundDirection;
        return true;
    }

    static int StairFacing(int direction) => direction switch {
        0 => 1,
        1 => 3,
        2 => 2,
        _ => 0
    };

    static int Opposite(int direction) => direction switch {
        0 => 1,
        1 => 0,
        2 => 3,
        _ => 2
    };

    static int OffsetX(int direction) => direction switch {
        0 => 1,
        1 => -1,
        _ => 0
    };

    static int OffsetZ(int direction) => direction switch {
        2 => 1,
        3 => -1,
        _ => 0
    };
}
