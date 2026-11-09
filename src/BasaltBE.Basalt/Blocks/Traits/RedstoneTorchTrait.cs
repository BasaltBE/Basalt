using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:redstone_torch", "minecraft:unlit_redstone_torch")]
public sealed class RedstoneTorchTrait : BlockSupportTrait {
    internal override bool HasScheduledUpdates => true;

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (face == 0 || face > 5) return false;

        string direction = BlockDirections.TorchFacingByFace[face];
        if (!BlockDirections.TorchSupportByDirection.TryGetValue(direction, out var support) ||
            BlockPalette.Find(permutation.Identifier, permutation.State, "torch_facing_direction", direction) is not { } faced ||
            BlockPalette.FindType(player.Dimension.GetPermutation(x + support.X, y + support.Y, z + support.Z).Identifier)?.Solid != true)
            return false;

        permutation = faced;
        return true;
    }

    public override void OnPlaced(Dimension dimension, int x, int y, int z) =>
        dimension.ScheduleBlockUpdate(x, y, z, 2);

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        bool? hasSupport = HasSupport(dimension, x, y, z);
        if (hasSupport is false) return base.OnNeighborUpdate(dimension, x, y, z);
        if (hasSupport is null) return false;

        dimension.ScheduleBlockUpdate(x, y, z, 2);
        return false;
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        if (!IsTorch(current.Identifier) ||
            current.State.Values.GetValueOrDefault("torch_facing_direction") is not string direction ||
            !BlockDirections.TorchSupportByDirection.TryGetValue(direction, out var support))
            return;

        bool? hasSupport = HasSupport(dimension, x, y, z);
        if (hasSupport is null) return;
        if (!hasSupport.Value) {
            base.OnNeighborUpdate(dimension, x, y, z);
            return;
        }

        bool lit = current.Identifier == "minecraft:redstone_torch";
        bool? powered = dimension.Redstone.BlockPowered(
            dimension,
            x + support.X,
            y + support.Y,
            z + support.Z
        );
        if (powered is null || lit != powered.Value) return;

        string identifier = lit ? "minecraft:unlit_redstone_torch" : "minecraft:redstone_torch";
        if (BlockPalette.Find(identifier, current.State) is not { } updated) return;

        dimension.SetPermutation(x, y, z, updated);
        dimension.Redstone.QueueUpdate(x, y, z, updated);
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? current = dimension.GetLoadedPermutation(x, y, z);
        if (current is null) return null;
        if (current.State.Values.GetValueOrDefault("torch_facing_direction") is not string direction ||
            !BlockDirections.TorchSupportByDirection.TryGetValue(direction, out var support))
            return false;

        BlockPermutation? supportBlock = dimension.GetLoadedPermutation(x + support.X, y + support.Y, z + support.Z);
        return supportBlock is null ? null : BlockPalette.FindType(supportBlock.Identifier)?.Solid == true;
    }

    static bool IsTorch(string identifier) => identifier is "minecraft:redstone_torch" or "minecraft:unlit_redstone_torch";
}
