using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Worlds.Dimensions;

internal enum PistonTransitionResult {
    Rejected,
    Retry,
    Committed
}

public sealed partial class Dimension {
    internal PistonTransitionResult MovePistonBlocks(
        ReadOnlySpan<(int X, int Y, int Z)> positions,
        (int X, int Y, int Z) offset,
        ReadOnlySpan<(int X, int Y, int Z)> broken,
        ReadOnlySpan<(int X, int Y, int Z, BlockPermutation Permutation)> changes,
        PistonArmBlockActor actor,
        bool extended
    ) {
        World?.CheckOwnership();
        ArgumentNullException.ThrowIfNull(actor);
        if (positions.Length > 12 || broken.Length > 64 || changes.Length > 2) return PistonTransitionResult.Rejected;

        PistonMove[] moves = new PistonMove[positions.Length];
        HashSet<(int X, int Y, int Z)> sources = [];
        HashSet<(int X, int Y, int Z)> destinations = [];
        HashSet<(int X, int Y, int Z)> breakSet = [];
        Dictionary<(int X, int Y, int Z), BlockPermutation> previous = [];
        HashSet<long> saveKeys = [];
        for (int index = 0; index < positions.Length; index++) {
            var source = positions[index];
            (int X, int Y, int Z) destination = (source.X + offset.X, source.Y + offset.Y, source.Z + offset.Z);
            if (source.Y < MinimumY || source.Y > MaximumY || destination.Y < MinimumY || destination.Y > MaximumY ||
                GetChunk(source.X >> 4, source.Z >> 4) is not { } sourceChunk ||
                GetChunk(destination.X >> 4, destination.Z >> 4) is not { } destinationChunk ||
                !sources.Add(source) || !destinations.Add(destination) ||
                sourceChunk.GetBlockNbt(source.X & 15, source.Y, source.Z & 15) is not null ||
                sourceChunk.GetRuntimeActor(source.X & 15, source.Y, source.Z & 15) is not null)
                return PistonTransitionResult.Rejected;

            BlockPermutation permutation = sourceChunk.GetPermutation(source.X & 15, source.Y, source.Z & 15);
            if (BlockPalette.Replaceable(permutation)) return PistonTransitionResult.Rejected;

            moves[index] = new PistonMove(source, destination, permutation, sourceChunk, destinationChunk);
            previous.TryAdd(source, permutation);
            previous.TryAdd(destination, destinationChunk.GetPermutation(destination.X & 15, destination.Y, destination.Z & 15));
        }

        foreach (var position in broken) {
            if (position.Y < MinimumY || position.Y > MaximumY || sources.Contains(position) ||
                GetChunk(position.X >> 4, position.Z >> 4) is not { } chunk || !breakSet.Add(position) ||
                chunk.GetBlockNbt(position.X & 15, position.Y, position.Z & 15) is not null ||
                chunk.GetRuntimeActor(position.X & 15, position.Y, position.Z & 15) is not null)
                return PistonTransitionResult.Rejected;

            BlockPermutation permutation = chunk.GetPermutation(position.X & 15, position.Y, position.Z & 15);
            previous.TryAdd(position, permutation);
        }

        foreach (PistonMove move in moves) {
            if (sources.Contains(move.Destination) || breakSet.Contains(move.Destination)) continue;

            BlockPermutation destination = move.DestinationChunk.GetPermutation(
                move.Destination.X & 15,
                move.Destination.Y,
                move.Destination.Z & 15
            );
            bool clearedByChange = false;
            if (!extended) {
                foreach (var change in changes) {
                    if (change.X == move.Destination.X && change.Y == move.Destination.Y && change.Z == move.Destination.Z &&
                        BlockPalette.Replaceable(change.Permutation)) {
                        clearedByChange = true;
                        break;
                    }
                }
            }
            if ((!BlockPalette.Replaceable(destination) && !clearedByChange) ||
                move.DestinationChunk.GetBlockNbt(move.Destination.X & 15, move.Destination.Y, move.Destination.Z & 15) is not null ||
                HasPendingBlockUpdate(move.Destination.X, move.Destination.Y, move.Destination.Z))
                return PistonTransitionResult.Rejected;
        }

        foreach (var change in changes) {
            if (change.Y < MinimumY || change.Y > MaximumY ||
                GetChunk(change.X >> 4, change.Z >> 4) is not { } chunk ||
                (extended && destinations.Contains((change.X, change.Y, change.Z))) ||
                (!extended && breakSet.Contains((change.X, change.Y, change.Z))) ||
                chunk.GetBlockNbt(change.X & 15, change.Y, change.Z & 15) is not null ||
                HasPendingBlockUpdate(change.X, change.Y, change.Z) &&
                    !(extended && (breakSet.Contains((change.X, change.Y, change.Z)) ||
                        sources.Contains((change.X, change.Y, change.Z)))))
                return PistonTransitionResult.Rejected;

            previous.TryAdd(
                (change.X, change.Y, change.Z),
                chunk.GetPermutation(change.X & 15, change.Y, change.Z & 15)
            );
        }

        if (actor.Position.Y < MinimumY || actor.Position.Y > MaximumY ||
            GetChunk(actor.Position.X >> 4, actor.Position.Z >> 4) is not { } actorChunk ||
            actorChunk.GetRuntimeActor(actor.Position.X & 15, actor.Position.Y, actor.Position.Z & 15) != actor)
            return PistonTransitionResult.Rejected;

        foreach ((int x, int y, int z) in previous.Keys) saveKeys.Add(GetKey(x >> 4, z >> 4));
        saveKeys.Add(GetKey(actor.Position.X >> 4, actor.Position.Z >> 4));
        if (actor.Permutation.State.Values.GetValueOrDefault("facing_direction") is not int actorFacing ||
            (uint)actorFacing >= BlockDirections.PistonOffsetByFacing.Length)
            return PistonTransitionResult.Rejected;
        (int X, int Y, int Z) actorDirection = BlockDirections.PistonOffsetByFacing[actorFacing];
        int actorHeadX = actor.Position.X + actorDirection.X;
        int actorHeadZ = actor.Position.Z + actorDirection.Z;
        saveKeys.Add(GetKey(actorHeadX >> 4, actorHeadZ >> 4));
        if (saveKeys.Count > 16) return PistonTransitionResult.Rejected;
        foreach (long key in saveKeys) {
            if ((scheduledChunks.TryGetValue(key, out ScheduledChunk? state) && state.HasUnindexedRecords) ||
                (chunks.TryGetValue(key, out Chunk? chunk) && chunk is { PendingTicksInvalid: true }))
                return PistonTransitionResult.Rejected;
        }
        if (!GroupChunkSaves([.. saveKeys])) return PistonTransitionResult.Retry;

        foreach ((int x, int y, int z) in breakSet) CancelScheduledBlockUpdate(x, y, z);
        foreach (PistonMove move in moves)
            move.SourceChunk.SetPermutation(move.Source.X & 15, move.Source.Y, move.Source.Z & 15, BlockStorage.Air);
        foreach ((int x, int y, int z) in breakSet) GetChunk(x >> 4, z >> 4)!.SetPermutation(x & 15, y, z & 15, BlockStorage.Air);
        if (!extended) {
            foreach (var change in changes)
                GetChunk(change.X >> 4, change.Z >> 4)!.SetPermutation(
                    change.X & 15,
                    change.Y,
                    change.Z & 15,
                    change.Permutation
                );
        }
        foreach (PistonMove move in moves) {
            move.DestinationChunk.SetPermutation(
                move.Destination.X & 15,
                move.Destination.Y,
                move.Destination.Z & 15,
                move.Permutation
            );
        }
        MoveBlockTicks(moves);
        if (extended) {
            foreach (var change in changes)
                GetChunk(change.X >> 4, change.Z >> 4)!.SetPermutation(
                    change.X & 15,
                    change.Y,
                    change.Z & 15,
                    change.Permutation
                );
        }

        actor.State = extended ? (byte)2 : (byte)0;

        foreach (((int x, int y, int z), BlockPermutation before) in previous)
            Redstone.BlockChanged(this, x, y, z, before, GetPermutation(x, y, z));

        HashSet<(int X, int Y, int Z)> neighbors = [];
        foreach ((int x, int y, int z) in previous.Keys) {
            if (BlockPalette.OnNeighborUpdate(this, x, y, z)) {
                Redstone.MarkDirty(x, y, z);
            }
            BroadcastBlockUpdate(x, y, z, GetPermutation(x, y, z));
            neighbors.Add((x + 1, y, z));
            neighbors.Add((x - 1, y, z));
            neighbors.Add((x, y, z + 1));
            neighbors.Add((x, y, z - 1));
            neighbors.Add((x, y + 1, z));
            neighbors.Add((x, y - 1, z));
        }

        foreach ((int x, int y, int z) in neighbors) {
            if (!BlockPalette.OnNeighborUpdate(this, x, y, z)) continue;

            BroadcastBlockUpdate(x, y, z, GetPermutation(x, y, z));
            Redstone.MarkDirty(x, y, z);
        }

        actor.Update();
        return PistonTransitionResult.Committed;
    }

    internal bool CanRemovePistonBase(PistonArmBlockActor actor) {
        if (actor.Permutation.State.Values.GetValueOrDefault("facing_direction") is not int facing ||
            (uint)facing >= BlockDirections.PistonOffsetByFacing.Length)
            return !actor.Extended;

        (int X, int Y, int Z) direction = BlockDirections.PistonOffsetByFacing[facing];
        int headX = actor.Position.X + direction.X;
        int headY = actor.Position.Y + direction.Y;
        int headZ = actor.Position.Z + direction.Z;
        if (headY < MinimumY || headY > MaximumY || GetChunk(headX >> 4, headZ >> 4) is not { } headChunk) return !actor.Extended;

        BlockPermutation head = headChunk.GetPermutation(headX & 15, headY, headZ & 15);
        if (head.Identifier is not ("minecraft:sticky_piston_arm_collision" or "minecraft:piston_arm_collision")) return true;

        return GroupChunkSaves([
            GetKey(actor.Position.X >> 4, actor.Position.Z >> 4),
            GetKey(headX >> 4, headZ >> 4)
        ]);
    }

    internal void BreakPistonBase(PistonArmBlockActor actor) {
        World?.CheckOwnership();
        Redstone.ClearPistonWait(actor.Position.X, actor.Position.Y, actor.Position.Z);
        if (actor.Permutation.State.Values.GetValueOrDefault("facing_direction") is not int facing ||
            (uint)facing >= BlockDirections.PistonOffsetByFacing.Length) {
            UpdateNeighbors(actor.Position.X, actor.Position.Y, actor.Position.Z);
            return;
        }

        (int X, int Y, int Z) direction = BlockDirections.PistonOffsetByFacing[facing];
        int headX = actor.Position.X + direction.X;
        int headY = actor.Position.Y + direction.Y;
        int headZ = actor.Position.Z + direction.Z;
        if (headY < MinimumY || headY > MaximumY || GetChunk(headX >> 4, headZ >> 4) is not { } headChunk) {
            UpdateNeighbors(actor.Position.X, actor.Position.Y, actor.Position.Z);
            return;
        }

        BlockPermutation head = headChunk.GetPermutation(headX & 15, headY, headZ & 15);
        if (head.Identifier is not ("minecraft:sticky_piston_arm_collision" or "minecraft:piston_arm_collision")) {
            UpdateNeighbors(actor.Position.X, actor.Position.Y, actor.Position.Z);
            return;
        }

        headChunk.SetPermutation(headX & 15, headY, headZ & 15, BlockStorage.Air);
        Redstone.BlockChanged(this, headX, headY, headZ, head, BlockStorage.Air);
        BroadcastBlockUpdate(headX, headY, headZ, BlockStorage.Air);
        UpdateNeighbors(headX, headY, headZ);
        UpdateNeighbors(actor.Position.X, actor.Position.Y, actor.Position.Z);
    }

    readonly record struct PistonMove(
        (int X, int Y, int Z) Source,
        (int X, int Y, int Z) Destination,
        BlockPermutation Permutation,
        Chunk SourceChunk,
        Chunk DestinationChunk
    );
}
