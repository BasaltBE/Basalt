using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:piston", "minecraft:sticky_piston")]
public sealed class PistonTrait : BlockTrait {
    const int MaximumMovedBlocks = 12;
    const int MaximumVisitedPositions = 64;
    internal override bool HasScheduledUpdates => true;

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        int facing = BlockDirections.FacingByRotation(player.Rotation.X, player.Rotation.Y);
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "facing_direction", facing) ?? permutation;
        return permutation.State.Values.GetValueOrDefault("facing_direction") is int placedFacing && placedFacing == facing;
    }

    public override void OnPlaced(Dimension dimension, int x, int y, int z) => dimension.ScheduleBlockUpdate(x, y, z, 1);

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) => Update(dimension, x, y, z);

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) => Update(dimension, x, y, z);

    static bool Update(Dimension dimension, int x, int y, int z) {
        BlockPermutation piston = dimension.GetPermutation(x, y, z);
        if (piston.State.Values.GetValueOrDefault("facing_direction") is not int facing ||
            (uint)facing >= BlockDirections.PistonOffsetByFacing.Length ||
            dimension.GetChunk(x >> 4, z >> 4) is not { } chunk ||
            dimension.GetBlockActor(chunk, x & 15, y, z & 15) is not PistonArmBlockActor actor)
            return false;

        dimension.Redstone.ClearPistonWait(x, y, z);
        if (dimension.Redstone.BlockPowered(dimension, x, y, z) is not bool powered)
            return false;

        actor.Sticky = piston.Identifier == "minecraft:sticky_piston";
        (int X, int Y, int Z) direction = BlockDirections.PistonOffsetByFacing[facing];
        int headX = x + direction.X;
        int headY = y + direction.Y;
        int headZ = z + direction.Z;
        BlockPermutation head = dimension.GetPermutation(headX, headY, headZ);
        string expectedHead = actor.Sticky ? "minecraft:sticky_piston_arm_collision" : "minecraft:piston_arm_collision";
        bool hasHead = head.Identifier == expectedHead;

        if (actor.Extended != hasHead) {
            if (actor.Extended && !hasHead) {
                PistonTransitionResult transition = dimension.MovePistonBlocks([], default, [], [], actor, false);
                if (transition == PistonTransitionResult.Retry)
                    dimension.ScheduleBlockUpdate(x, y, z, 20);
                if (transition != PistonTransitionResult.Committed)
                    return false;
                if (powered)
                    dimension.ScheduleBlockUpdate(x, y, z, 1);
                return true;
            }

            if (hasHead && !powered) {
                PistonTransitionResult transition = dimension.MovePistonBlocks(
                    [],
                    default,
                    [],
                    [(headX, headY, headZ, BlockStorage.Air)],
                    actor,
                    false
                );
                if (transition == PistonTransitionResult.Retry)
                    dimension.ScheduleBlockUpdate(x, y, z, 20);
                if (transition != PistonTransitionResult.Committed)
                    return false;
                return true;
            }

            PistonTransitionResult reconciliation = dimension.MovePistonBlocks([], default, [], [], actor, hasHead);
            if (reconciliation == PistonTransitionResult.Retry)
                dimension.ScheduleBlockUpdate(x, y, z, 20);
            if (reconciliation != PistonTransitionResult.Committed)
                return false;
            if (hasHead)
                return true;
        }

        if (powered == actor.Extended)
            return false;

        PistonTransitionResult result = powered
            ? Extend(dimension, x, y, z, direction, facing, actor)
            : Retract(dimension, x, y, z, direction, actor);
        if (result == PistonTransitionResult.Retry)
            dimension.ScheduleBlockUpdate(x, y, z, 20);
        return result == PistonTransitionResult.Committed;
    }

    static PistonTransitionResult Extend(
        Dimension dimension,
        int x,
        int y,
        int z,
        (int X, int Y, int Z) direction,
        int facing,
        PistonArmBlockActor actor
    ) {
        List<(int X, int Y, int Z)> moving = [];
        List<(int X, int Y, int Z)> broken = [];
        List<(int X, int Y, int Z, BlockPermutation Block)> drops = [];
        Queue<(int X, int Y, int Z)> pending = [];
        HashSet<(int X, int Y, int Z)> visited = [];
        pending.Enqueue((x + direction.X, y + direction.Y, z + direction.Z));

        while (pending.TryDequeue(out var position)) {
            if (position == (x, y, z) || !visited.Add(position))
                continue;
            if (visited.Count > MaximumVisitedPositions || position.Y < dimension.MinimumY || position.Y > dimension.MaximumY)
                return PistonTransitionResult.Rejected;
            if (dimension.GetLoadedPermutation(position.X, position.Y, position.Z) is not { } block) {
                if (dimension.GetChunk(position.X >> 4, position.Z >> 4) is null)
                    dimension.Redstone.WaitForPiston(x, y, z, position.X >> 4, position.Z >> 4);
                return PistonTransitionResult.Rejected;
            }

            string? movement = BlockPalette.FindType(block.Identifier)?.Components.Movable?.MovementType;
            if (movement == "Popped") {
                broken.Add(position);
                drops.Add((position.X, position.Y, position.Z, block));
                pending.Enqueue((position.X + direction.X, position.Y + direction.Y, position.Z + direction.Z));
                continue;
            }
            if (BlockPalette.Replaceable(block))
                continue;
            if (moving.Count == MaximumMovedBlocks || movement is not ("PushPull" or "Push"))
                return PistonTransitionResult.Rejected;

            moving.Add(position);
            pending.Enqueue((position.X + direction.X, position.Y + direction.Y, position.Z + direction.Z));

            if (BlockPalette.FindType(block.Identifier)?.Components.Movable?.StickyType is not ("Same" or "All"))
                continue;

            foreach ((int X, int Y, int Z) offset in BlockDirections.OffsetByDirection.Values) {
                var attached = (position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z);
                if (attached == (x, y, z))
                    continue;
                if (attached.Item2 < dimension.MinimumY || attached.Item2 > dimension.MaximumY)
                    return PistonTransitionResult.Rejected;
                if (dimension.GetLoadedPermutation(attached.Item1, attached.Item2, attached.Item3) is not { } attachedBlock) {
                    if (dimension.GetChunk(attached.Item1 >> 4, attached.Item3 >> 4) is null)
                        dimension.Redstone.WaitForPiston(x, y, z, attached.Item1 >> 4, attached.Item3 >> 4);
                    return PistonTransitionResult.Rejected;
                }
                string? attachedMovement = BlockPalette.FindType(attachedBlock.Identifier)?.Components.Movable?.MovementType;
                if (BlockPalette.Replaceable(attachedBlock) && attachedMovement != "Popped")
                    continue;
                if ((block.Identifier == "minecraft:slime" && attachedBlock.Identifier == "minecraft:honey_block") ||
                    (block.Identifier == "minecraft:honey_block" && attachedBlock.Identifier == "minecraft:slime"))
                    continue;
                pending.Enqueue(attached);
            }
        }

        string headIdentifier = actor.Sticky ? "minecraft:sticky_piston_arm_collision" : "minecraft:piston_arm_collision";
        if (BlockPalette.FindDefaultPermutation(headIdentifier) is not { } head ||
            BlockPalette.Find(headIdentifier, head.State, "facing_direction", facing) is not { } facedHead)
            return PistonTransitionResult.Rejected;

        int headX = x + direction.X;
        int headY = y + direction.Y;
        int headZ = z + direction.Z;
        PistonTransitionResult result = dimension.MovePistonBlocks(
            [.. moving],
            direction,
            [.. broken],
            [(headX, headY, headZ, facedHead)],
            actor,
            true
        );
        if (result != PistonTransitionResult.Committed)
            return result;

        foreach (var drop in drops)
            Drop(dimension, drop.Block, drop.X, drop.Y, drop.Z);
        PlaySound(dimension, x, y, z, "piston.out");
        return PistonTransitionResult.Committed;
    }

    static PistonTransitionResult Retract(
        Dimension dimension,
        int x,
        int y,
        int z,
        (int X, int Y, int Z) direction,
        PistonArmBlockActor actor
    ) {
        int headX = x + direction.X;
        int headY = y + direction.Y;
        int headZ = z + direction.Z;
        BlockPermutation head = dimension.GetPermutation(headX, headY, headZ);
        if (head.Identifier is not ("minecraft:piston_arm_collision" or "minecraft:sticky_piston_arm_collision"))
            return dimension.MovePistonBlocks([], default, [], [], actor, false);

        List<(int X, int Y, int Z)> moving = [];
        List<(int X, int Y, int Z)> broken = [];
        List<(int X, int Y, int Z, BlockPermutation Block)> drops = [];
        if (actor.Sticky) {
            int sourceX = headX + direction.X;
            int sourceY = headY + direction.Y;
            int sourceZ = headZ + direction.Z;
            if (sourceY >= dimension.MinimumY && sourceY <= dimension.MaximumY &&
                dimension.GetLoadedPermutation(sourceX, sourceY, sourceZ) is { } block &&
                !BlockPalette.Replaceable(block) &&
                BlockPalette.FindType(block.Identifier)?.Components.Movable?.MovementType == "PushPull" &&
                dimension.GetChunk(sourceX >> 4, sourceZ >> 4) is { } sourceChunk &&
                sourceChunk.GetBlockNbt(sourceX & 15, sourceY, sourceZ & 15) is null &&
                sourceChunk.GetRuntimeActor(sourceX & 15, sourceY, sourceZ & 15) is null) {
                Queue<(int X, int Y, int Z)> pending = [];
                HashSet<(int X, int Y, int Z)> visited = [];
                pending.Enqueue((sourceX, sourceY, sourceZ));
                bool canPull = true;

                while (pending.TryDequeue(out var position)) {
                    if (!visited.Add(position))
                        continue;
                    if (visited.Count > MaximumVisitedPositions || position.Y < dimension.MinimumY ||
                        position.Y > dimension.MaximumY ||
                        dimension.GetLoadedPermutation(position.X, position.Y, position.Z) is not { } movedBlock ||
                        dimension.GetChunk(position.X >> 4, position.Z >> 4) is not { } movedChunk ||
                        movedChunk.GetBlockNbt(position.X & 15, position.Y, position.Z & 15) is not null ||
                        movedChunk.GetRuntimeActor(position.X & 15, position.Y, position.Z & 15) is not null) {
                        canPull = false;
                        break;
                    }

                    string? movement = BlockPalette.FindType(movedBlock.Identifier)?.Components.Movable?.MovementType;
                    if (movement == "Popped") {
                        broken.Add(position);
                        drops.Add((position.X, position.Y, position.Z, movedBlock));
                        continue;
                    }
                    if (movement != "PushPull") {
                        canPull = false;
                        break;
                    }

                    moving.Add(position);
                    if (moving.Count > MaximumMovedBlocks)
                        canPull = false;
                    if (!canPull || BlockPalette.FindType(movedBlock.Identifier)?.Components.Movable?.StickyType is not ("Same" or "All"))
                        continue;

                    foreach ((int X, int Y, int Z) offset in BlockDirections.OffsetByDirection.Values) {
                        var attached = (position.X + offset.X, position.Y + offset.Y, position.Z + offset.Z);
                        if (attached == (x, y, z) || attached == (headX, headY, headZ))
                            continue;
                        if (dimension.GetLoadedPermutation(attached.Item1, attached.Item2, attached.Item3) is not { } attachedBlock) {
                            canPull = false;
                            break;
                        }
                        string? attachedMovement = BlockPalette.FindType(attachedBlock.Identifier)?.Components.Movable?.MovementType;
                        if (BlockPalette.Replaceable(attachedBlock) && attachedMovement != "Popped")
                            continue;
                        if ((movedBlock.Identifier == "minecraft:slime" && attachedBlock.Identifier == "minecraft:honey_block") ||
                            (movedBlock.Identifier == "minecraft:honey_block" && attachedBlock.Identifier == "minecraft:slime"))
                            continue;
                        pending.Enqueue(attached);
                    }
                    if (!canPull)
                        break;
                }

                if (!canPull) {
                    moving.Clear();
                    broken.Clear();
                    drops.Clear();
                }
            }
        }

        if (moving.Count > 0) {
            foreach (var position in moving) {
                int destinationX = position.X - direction.X;
                int destinationY = position.Y - direction.Y;
                int destinationZ = position.Z - direction.Z;
                if (destinationY < dimension.MinimumY || destinationY > dimension.MaximumY ||
                    dimension.GetChunk(destinationX >> 4, destinationZ >> 4) is not { } destinationChunk) {
                    moving.Clear();
                    broken.Clear();
                    drops.Clear();
                    break;
                }

                if ((destinationX, destinationY, destinationZ) == (headX, headY, headZ) ||
                    moving.Contains((destinationX, destinationY, destinationZ)))
                    continue;

                BlockPermutation destination = destinationChunk.GetPermutation(destinationX & 15, destinationY, destinationZ & 15);
                if (!BlockPalette.Replaceable(destination) ||
                    destinationChunk.GetBlockNbt(destinationX & 15, destinationY, destinationZ & 15) is not null ||
                    destinationChunk.GetRuntimeActor(destinationX & 15, destinationY, destinationZ & 15) is not null ||
                    dimension.HasPendingBlockUpdate(destinationX, destinationY, destinationZ)) {
                    moving.Clear();
                    broken.Clear();
                    drops.Clear();
                    break;
                }
            }
        }

        PistonTransitionResult result = dimension.MovePistonBlocks(
            [.. moving],
            (-direction.X, -direction.Y, -direction.Z),
            [.. broken],
            [(headX, headY, headZ, BlockStorage.Air)],
            actor,
            false
        );
        if (result != PistonTransitionResult.Committed)
            return result;

        foreach (var drop in drops)
            Drop(dimension, drop.Block, drop.X, drop.Y, drop.Z);
        PlaySound(dimension, x, y, z, "piston.in");
        return PistonTransitionResult.Committed;
    }

    static void PlaySound(Dimension dimension, int x, int y, int z, string sound) =>
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = sound,
            Position = new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
            ActorUniqueId = -1
        }, x >> 4, z >> 4);

    static void Drop(Dimension dimension, BlockPermutation block, int x, int y, int z) {
        if (dimension.World is not { } world)
            return;

        foreach (BlockDrop drop in world.LootTables.Get(block.Identifier)) {
            ItemType? itemType = ItemPalette.Find(drop.Identifier);
            if (itemType is null || itemType.NetworkId == 0 || itemType.MaxAmount is <= 0 or > ushort.MaxValue)
                continue;

            int amount = drop.MinAmount == drop.MaxAmount
                ? drop.MinAmount
                : (int)Random.Shared.NextInt64(drop.MinAmount, (long)drop.MaxAmount + 1);
            while (amount > 0) {
                ushort stackSize = (ushort)Math.Min(itemType.MaxAmount, amount);
                new ItemEntity(new ItemStack(itemType, stackSize), x, y, z).Spawn(dimension);
                amount -= stackSize;
            }
        }
    }
}
