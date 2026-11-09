using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo(
    "minecraft:unpowered_repeater",
    "minecraft:powered_repeater",
    "minecraft:unpowered_comparator",
    "minecraft:powered_comparator"
)]
public sealed class RedstoneDeviceTrait : BlockSupportTrait {
    internal override bool HasScheduledUpdates => true;

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        int facing = (int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3;
        if (face != 1 ||
            BlockPalette.Find(permutation.Identifier, permutation.State, "minecraft:cardinal_direction", BlockDirections.CardinalDirectionByYaw[facing]) is not { } faced)
            return false;

        permutation = faced;
        return HasSupport(player.Dimension, x, y, z) is true;
    }

    public override void OnPlaced(Dimension dimension, int x, int y, int z) {
        BlockPermutation permutation = dimension.GetPermutation(x, y, z);
        if (permutation.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
            dimension.Redstone.ScheduleRepeaterUpdate(dimension, x, y, z, permutation);
        else dimension.ScheduleBlockUpdate(x, y, z, 2);
    }

    public override bool OnInteract(Player player, BlockPos position) {
        Dimension dimension = player.Dimension;
        BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
        BlockPermutation? updated = null;
        if (current.Identifier is "minecraft:unpowered_repeater" or "minecraft:powered_repeater") {
            int delay = current.State.Values.GetValueOrDefault("repeater_delay") is int value ? (value + 1) & 3 : 0;
            updated = BlockPalette.Find(current.Identifier, current.State, "repeater_delay", delay);
        } else if ((current.Identifier is "minecraft:unpowered_comparator" or "minecraft:powered_comparator") &&
                   current.State.Values.GetValueOrDefault("output_subtract_bit") is bool subtract) {
            updated = BlockPalette.Find(current.Identifier, current.State, "output_subtract_bit", !subtract);
        }

        if (updated is null) return false;

        dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, updated);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "random.click",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        }, position.X >> 4, position.Z >> 4);
        if (updated.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
            dimension.Redstone.ScheduleRepeaterUpdate(dimension, position.X, position.Y, position.Z, updated);
        else dimension.ScheduleBlockUpdate(position.X, position.Y, position.Z, 1);
        return true;
    }

    internal override void OnScheduledUpdate(
        Dimension dimension,
        int x,
        int y,
        int z,
        BlockPermutation current,
        BlockPermutation scheduled
    ) {
        if ((current.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater") &&
            (scheduled.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")) {
            if (current.Identifier == scheduled.Identifier) return;

            if (BlockPalette.Find(scheduled.Identifier, current.State) is not { } repeater) return;

            dimension.SetPermutation(x, y, z, repeater);
            dimension.BroadcastBlockUpdate(x, y, z, repeater);
            dimension.UpdateNeighbors(x, y, z);
            return;
        }

        base.OnScheduledUpdate(dimension, x, y, z, current, scheduled);
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        if (current.Identifier is "minecraft:unpowered_repeater" or "minecraft:powered_repeater") {
            if (current.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string inputDirection ||
                !BlockDirections.OffsetByCardinalDirection.TryGetValue(inputDirection, out var inputOffset))
                return;

            int? inputPower = dimension.Redstone.InputPower(
                dimension,
                x + inputOffset.X,
                y + inputOffset.Y,
                z + inputOffset.Z,
                x,
                y,
                z
            );
            bool? locked = dimension.Redstone.RepeaterLocked(dimension, x, y, z, inputOffset);
            if (inputPower is null || locked is null) return;

            bool powered = inputPower > 0 && !locked.Value;
            string identifier = powered ? "minecraft:powered_repeater" : "minecraft:unpowered_repeater";
            if (current.Identifier == identifier) return;

            if (BlockPalette.Find(identifier, current.State) is not { } repeater) return;

            dimension.SetPermutation(x, y, z, repeater);
            dimension.BroadcastBlockUpdate(x, y, z, repeater);
            dimension.UpdateNeighbors(x, y, z);
            return;
        }

        if (current.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string comparatorBackDirection ||
            !BlockDirections.OffsetByCardinalDirection.TryGetValue(comparatorBackDirection, out var comparatorBack))
            return;

        int? comparatorInput = dimension.Redstone.InputPower(
            dimension,
            x + comparatorBack.X,
            y + comparatorBack.Y,
            z + comparatorBack.Z,
            x,
            y,
            z
        );
        int? sideInput = dimension.Redstone.ComparatorSidePower(dimension, x, y, z, comparatorBack);
        if (comparatorInput is null || sideInput is null) return;

        bool subtract = current.State.Values.GetValueOrDefault("output_subtract_bit") is true;
        int output = comparatorInput.Value < sideInput.Value
            ? 0
            : subtract ? comparatorInput.Value - sideInput.Value : comparatorInput.Value;
        dimension.Redstone.SetComparatorPower(dimension, x, y, z, output);

        bool lit = output > 0;
        string comparatorIdentifier = lit ? "minecraft:powered_comparator" : "minecraft:unpowered_comparator";
        if (current.Identifier == comparatorIdentifier &&
            current.State.Values.GetValueOrDefault("output_lit_bit") is bool currentLit && currentLit == lit)
            return;

        if (BlockPalette.Find(comparatorIdentifier, current.State, "output_lit_bit", lit) is not { } comparator) return;

        dimension.SetPermutation(x, y, z, comparator);
        dimension.BroadcastBlockUpdate(x, y, z, comparator);
        dimension.UpdateNeighbors(x, y, z);
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? support = dimension.GetLoadedPermutation(x, y - 1, z);
        return support is null ? null : BlockPalette.FindType(support.Identifier)?.Solid == true;
    }
}
