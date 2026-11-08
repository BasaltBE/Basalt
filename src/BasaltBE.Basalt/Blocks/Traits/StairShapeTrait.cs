using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Blocks.Traits;

public sealed class StairShapeTrait : BlockTrait {
    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (current.State.Values.GetValueOrDefault("weirdo_direction") is not int direction ||
            (uint)direction >= (uint)BlockDirections.StairFacingByDirection.Length)
            return false;

        if (current.State.Values.GetValueOrDefault("upside_down_bit") is not bool upsideDown) return false;


        string corner = FindCorner(dimension, x, y, z, direction, upsideDown);
        if (current.State.Values.GetValueOrDefault("minecraft:corner") is string currentCorner && currentCorner == corner) return false;

        BlockPermutation? updated = BlockPalette.Find(current.Identifier, current.State, "minecraft:corner", corner);
        if (updated is null) return false;

        dimension.SetPermutation(x, y, z, updated);
        return true;
    }

    static string FindCorner(Dimension dimension, int x, int y, int z, int direction, bool upsideDown) {
        int frontX = x + BlockDirections.StairOffsetXByDirection[direction];
        int frontZ = z + BlockDirections.StairOffsetZByDirection[direction];
        if (StairTurn(dimension, frontX, y, frontZ, upsideDown, direction, out int frontDirection) &&
            !StairShapeBlocked(dimension, x, y, z, direction, frontDirection, upsideDown, outer: true)) {
            int turn = (BlockDirections.StairFacingByDirection[frontDirection] - BlockDirections.StairFacingByDirection[direction] + 4) & 3;
            return turn == 3 ? "outer_left" : "outer_right";
        }

        int backDirection = BlockDirections.StairOppositeByDirection[direction];
        int backX = x + BlockDirections.StairOffsetXByDirection[backDirection];
        int backZ = z + BlockDirections.StairOffsetZByDirection[backDirection];
        if (StairTurn(dimension, backX, y, backZ, upsideDown, direction, out int rearDirection) &&
            !StairShapeBlocked(dimension, x, y, z, direction, rearDirection, upsideDown, outer: false)) {
            int turn = (BlockDirections.StairFacingByDirection[rearDirection] - BlockDirections.StairFacingByDirection[direction] + 4) & 3;
            return turn == 3 ? "inner_left" : "inner_right";
        }

        return "none";
    }

    static bool StairShapeBlocked(Dimension dimension, int x, int y, int z, int direction, int neighborDirection, bool upsideDown, bool outer) {
        int checkDirection = outer ? BlockDirections.StairOppositeByDirection[neighborDirection] : neighborDirection;
        int checkX = x + BlockDirections.StairOffsetXByDirection[checkDirection];
        int checkZ = z + BlockDirections.StairOffsetZByDirection[checkDirection];
        if (dimension.GetChunk(checkX >> 4, checkZ >> 4) is null) return false;

        BlockPermutation block = dimension.GetPermutation(checkX, y, checkZ);
        return BlockPalette.IsCornerableStairs(block.Identifier) &&
            block.State.Values.GetValueOrDefault("weirdo_direction") is int value && value == direction &&
            block.State.Values.GetValueOrDefault("upside_down_bit") is bool valueHalf && valueHalf == upsideDown;
    }

    static bool StairTurn(Dimension dimension, int x, int y, int z, bool upsideDown, int direction, out int neighborDirection) {
        neighborDirection = 0;
        if (dimension.GetChunk(x >> 4, z >> 4) is null) return false;

        BlockPermutation neighbor = dimension.GetPermutation(x, y, z);
        if (!BlockPalette.IsCornerableStairs(neighbor.Identifier) ||
            neighbor.State.Values.GetValueOrDefault("weirdo_direction") is not int foundDirection ||
            (uint)foundDirection >= (uint)BlockDirections.StairFacingByDirection.Length ||
            neighbor.State.Values.GetValueOrDefault("upside_down_bit") is not bool foundUpsideDown || foundUpsideDown != upsideDown)
            return false;

        int turn = (BlockDirections.StairFacingByDirection[foundDirection] - BlockDirections.StairFacingByDirection[direction] + 4) & 3;
        if (turn is not (1 or 3)) return false;

        neighborDirection = foundDirection;
        return true;
    }
}
