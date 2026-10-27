using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:daylight_detector", "minecraft:daylight_detector_inverted")]
public sealed class DaylightDetectorTrait : BlockSupportTrait {
    internal override bool HasScheduledUpdates => true;

    public override bool OnInteract(Player player, BlockPos position) {
        Dimension dimension = player.Dimension;
        BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
        string identifier = current.Identifier == "minecraft:daylight_detector"
            ? "minecraft:daylight_detector_inverted"
            : "minecraft:daylight_detector";
        if (BlockPalette.Find(identifier, current.State) is not { } updated)
            return false;

        dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, updated);
        dimension.UpdateNeighbors(position.X, position.Y, position.Z);
        dimension.ScheduleBlockUpdate(position.X, position.Y, position.Z, 1);
        return true;
    }

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (face != 1 || HasSupport(player.Dimension, x, y, z) is not true)
            return false;

        return true;
    }

    public override void OnPlaced(Dimension dimension, int x, int y, int z) =>
        dimension.ScheduleBlockUpdate(x, y, z, 1);

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        long time = dimension.World is { } world
            ? world.GameTime % 24000
            : (long)(dimension.TickValue % 24000);
        if (time < 0)
            time += 24000;
        double daylight = time <= 12000
            ? Math.Sin(Math.PI * time / 12000)
            : 0;
        int signal = (int)Math.Round(daylight * 15);
        if (current.Identifier == "minecraft:daylight_detector_inverted")
            signal = 15 - signal;

        if (current.State.Values.GetValueOrDefault("redstone_signal") is not int currentSignal || currentSignal != signal) {
            if (BlockPalette.Find(current.Identifier, current.State, "redstone_signal", signal) is { } updated) {
                dimension.SetPermutation(x, y, z, updated);
                dimension.BroadcastBlockUpdate(x, y, z, updated);
                dimension.UpdateNeighbors(x, y, z);
            }
        }

        dimension.ScheduleBlockUpdate(x, y, z, 20);
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? support = dimension.GetLoadedPermutation(x, y - 1, z);
        return support is null ? null : BlockPalette.FindType(support.Identifier)?.Solid == true;
    }
}
