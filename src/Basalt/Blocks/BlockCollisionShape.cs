namespace Basalt.Core.Blocks;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Types;

public static class BlockCollisionShape {
    private static readonly CollisionBox FullCube = new(-8f, 0f, -8f, 16f, 16f, 16f);
    private static readonly CollisionBox[] EmptyBoxes = [];
    private static readonly CollisionBox[] FullCubeBoxes = [FullCube];

    public static IReadOnlyList<CollisionBox> GetBoxes(BlockPermutation permutation) {
        return GetBoxArray(permutation);
    }

    internal static CollisionBox[] GetBoxArray(BlockPermutation permutation) {
        if (permutation.CollisionBoxes is { } cached) {
            return cached;
        }

        CollisionBox[] resolved = ResolveBoxes(permutation);
        Interlocked.CompareExchange(ref permutation.CollisionBoxes, resolved, null);
        return permutation.CollisionBoxes!;
    }

    private static CollisionBox[] ResolveBoxes(BlockPermutation permutation) {
        BlockType type = permutation.Type;
        if (type.Air || type.Liquid || type.Water || type.Lava) {
            return EmptyBoxes;
        }

        if (type.GetComponent<CollisionBoxComponent>() is { } component) {
            return component.Enabled ? component.Boxes : [];
        }

        if (type.Identifier.EndsWith("_double_slab", StringComparison.Ordinal) ||
            type.Identifier.EndsWith("_double_stone_slab", StringComparison.Ordinal)) {
            return FullCubeBoxes;
        }

        if (type.Identifier.EndsWith("_slab", StringComparison.Ordinal) &&
            permutation.State.TryGetValue("minecraft:vertical_half", out BlockStateValue half)) {
            return [half.Kind == 1 && half.AsString() == "top"
                ? new CollisionBox(-8f, 8f, -8f, 16f, 8f, 16f)
                : new CollisionBox(-8f, 0f, -8f, 16f, 8f, 16f)];
        }

        if (type.Identifier.EndsWith("_trapdoor", StringComparison.Ordinal) ||
            type.Identifier == "minecraft:trapdoor") {
            return TrapdoorBoxes(permutation);
        }

        return type.Solid ? FullCubeBoxes : EmptyBoxes;
    }

    private static CollisionBox[] TrapdoorBoxes(BlockPermutation permutation) {
        bool open = ReadBool(permutation, "open_bit");
        bool upsideDown = ReadBool(permutation, "upside_down_bit");
        int direction = ReadNumber(permutation, "direction");

        if (!open) {
            return [upsideDown
                ? new CollisionBox(-8f, 13f, -8f, 16f, 3f, 16f)
                : new CollisionBox(-8f, 0f, -8f, 16f, 3f, 16f)];
        }

        return direction is 0 or 2
            ? [new CollisionBox(-8f, 0f, direction == 0 ? 13f : 0f, 16f, 16f, 3f)]
            : [new CollisionBox(direction == 1 ? 13f : 0f, 0f, -8f, 3f, 16f, 16f)];
    }

    private static bool ReadBool(BlockPermutation permutation, string name) {
        return permutation.State.TryGetValue(name, out BlockStateValue value) &&
            value.Kind == 2 && value.AsBool();
    }

    private static int ReadNumber(BlockPermutation permutation, string name) {
        return permutation.State.TryGetValue(name, out BlockStateValue value) &&
            value.Kind == 0
            ? (int)value.AsNumber()
            : 0;
    }
}
