using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Worlds.Dimensions;

internal sealed class RedstoneController {
    readonly HashSet<Position> dirty = [];
    readonly HashSet<Position> connectionDirty = [];
    readonly HashSet<Position> chunkLoadRebuild = [];
    readonly HashSet<Position> visited = [];
    readonly HashSet<Component> invalidComponents = [];
    readonly HashSet<Component> dirtyComponents = [];
    readonly Dictionary<Position, WireNode> components = [];
    readonly Dictionary<Position, int> wireIndices = [];
    readonly Dictionary<Position, byte> sourcePowers = [];
    readonly Dictionary<Position, byte> comparatorPowers = [];
    readonly Queue<Position> wiresToVisit = new();
    readonly Queue<int>[] powerBuckets = Enumerable.Range(0, 16).Select(_ => new Queue<int>()).ToArray();
    readonly List<Position> wires = [];
    readonly List<Position> unknownBoundaryWires = [];
    readonly BlockState connectionChanges = new();
    readonly Dictionary<(int X, int Y, int Z), RedstoneUpdateBatch> updates = [];
    readonly Stack<RedstoneUpdateBatch> reusableUpdates = [];
    readonly Dictionary<Position, (int X, int Z)> pistonWaitChunks = [];
    readonly Dictionary<(int X, int Z), HashSet<Position>> pistonsWaitingByChunk = [];

    internal void MarkDirty(int x, int y, int z) {
        dirty.Add(new Position(x, y, z));
    }

    internal void ClearPistonWait(int x, int y, int z) {
        Position piston = new(x, y, z);
        if (!pistonWaitChunks.Remove(piston, out (int X, int Z) missingChunk) ||
            !pistonsWaitingByChunk.TryGetValue(missingChunk, out HashSet<Position>? waiting))
            return;

        waiting.Remove(piston);
        if (waiting.Count == 0)
            pistonsWaitingByChunk.Remove(missingChunk);
    }

    internal void WaitForPiston(int pistonX, int pistonY, int pistonZ, int chunkX, int chunkZ) {
        ClearPistonWait(pistonX, pistonY, pistonZ);
        Position piston = new(pistonX, pistonY, pistonZ);
        (int X, int Z) missingChunk = (chunkX, chunkZ);
        pistonWaitChunks.Add(piston, missingChunk);
        if (!pistonsWaitingByChunk.TryGetValue(missingChunk, out HashSet<Position>? waiting)) {
            waiting = [];
            pistonsWaitingByChunk.Add(missingChunk, waiting);
        }
        waiting.Add(piston);
    }

    internal void ChunkLoaded(Dimension dimension, Chunk chunk) {
        int minimumSubChunkY = dimension.MinimumY >> 4;
        int subChunkCount = dimension.HeightRange >> 4;
        for (int subChunkY = minimumSubChunkY; subChunkY < minimumSubChunkY + subChunkCount; subChunkY++) {
            foreach ((int X, int Z) offset in HorizontalOffsets) {
                Chunk? neighborChunk = dimension.GetChunk(chunk.X + offset.X, chunk.Z + offset.Z);
                if (neighborChunk is null)
                    continue;
                bool neighborHasRedstone = neighborChunk.GetSubChunk(subChunkY) is { HasRedstone: true };
                bool loadedHasRedstone = chunk.GetSubChunk(subChunkY) is { HasRedstone: true };
                if (!neighborHasRedstone && !loadedHasRedstone)
                    continue;

                int outside = offset.X != 0
                    ? chunk.X * 16 + (offset.X < 0 ? -1 : 16)
                    : chunk.Z * 16 + (offset.Z < 0 ? -1 : 16);
                for (int along = 0; along < 16; along++) {
                    for (int offsetY = 0; offsetY < 16; offsetY++) {
                        int y = (subChunkY << 4) + offsetY;
                        int x = offset.X != 0 ? outside : chunk.X * 16 + along;
                        int z = offset.Z != 0 ? outside : chunk.Z * 16 + along;
                        BlockPermutation neighborBlock = dimension.GetPermutation(x, y, z);
                        if (neighborHasRedstone && IsWire(neighborBlock)) {
                            Position wire = new(x, y, z);
                            dirty.Add(wire);
                            connectionDirty.Add(wire);
                            if (components.TryGetValue(wire, out WireNode cached)) {
                                invalidComponents.Add(cached.Component);
                                foreach (Position member in cached.Component.Wires)
                                    chunkLoadRebuild.Add(member);
                            }
                        }

                        int loadedX = offset.X > 0 ? chunk.X * 16 : chunk.X * 16 + 15;
                        int loadedZ = offset.Z > 0 ? chunk.Z * 16 : chunk.Z * 16 + 15;
                        int edgeX = offset.X != 0 ? loadedX : chunk.X * 16 + along;
                        int edgeZ = offset.Z != 0 ? loadedZ : chunk.Z * 16 + along;
                        BlockPermutation loadedBlock = chunk.GetPermutation(edgeX & 15, y, edgeZ & 15);
                        if (loadedHasRedstone && IsBoundaryReactive(loadedBlock)) {
                            if (BlockPalette.OnNeighborUpdate(dimension, edgeX, y, edgeZ)) {
                                dimension.BroadcastBlockUpdate(edgeX, y, edgeZ, dimension.GetPermutation(edgeX, y, edgeZ));
                                MarkTopologyDirty(edgeX, y, edgeZ);
                            }
                            if (IsNeighborDependentDevice(loadedBlock.Identifier)) {
                                if (loadedBlock.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
                                    ScheduleRepeaterUpdate(dimension, edgeX, y, edgeZ, loadedBlock);
                                else
                                    dimension.ScheduleBlockUpdate(edgeX, y, edgeZ, DeviceDelay(loadedBlock));
                            }
                        }
                        if (neighborHasRedstone && IsBoundaryReactive(neighborBlock)) {
                            if (BlockPalette.OnNeighborUpdate(dimension, x, y, z)) {
                                dimension.BroadcastBlockUpdate(x, y, z, dimension.GetPermutation(x, y, z));
                                MarkTopologyDirty(x, y, z);
                            }
                        }
                        if (neighborHasRedstone && IsNeighborDependentDevice(neighborBlock.Identifier)) {
                            if (neighborBlock.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
                                ScheduleRepeaterUpdate(dimension, x, y, z, neighborBlock);
                            else
                                dimension.ScheduleBlockUpdate(x, y, z, DeviceDelay(neighborBlock));
                        }
                    }
                }
            }

            if (chunk.GetSubChunk(subChunkY) is not { HasRedstone: true })
                continue;

            int minimumY = subChunkY << 4;
            for (int x = 0; x < 16; x++) {
                for (int z = 0; z < 16; z++) {
                    for (int offsetY = 0; offsetY < 16; offsetY++) {
                        int y = minimumY + offsetY;
                        BlockPermutation block = chunk.GetPermutation(x, y, z);
                        if (IsWire(block)) {
                            int wireX = chunk.X * 16 + x;
                            int wireZ = chunk.Z * 16 + z;
                            Position position = new(wireX, y, wireZ);
                            dirty.Add(position);
                            connectionDirty.Add(position);
                            chunkLoadRebuild.Add(position);
                            for (int offsetX = -1; offsetX <= 1; offsetX++) {
                                for (int neighborY = y - 1; neighborY <= y + 1; neighborY++) {
                                    for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                                        Position neighbor = new(wireX + offsetX, neighborY, wireZ + offsetZ);
                                        if (!components.TryGetValue(neighbor, out WireNode wire))
                                            continue;

                                        Component component = wire.Component;
                                        bool pending = dirtyComponents.Remove(component) | invalidComponents.Remove(component);
                                        foreach (Position cached in component.Wires) {
                                            components.Remove(cached);
                                            sourcePowers.Remove(cached);
                                            chunkLoadRebuild.Add(cached);
                                            if (pending)
                                                dirty.Add(cached);
                                        }
                                    }
                                }
                            }
                            continue;
                        }

                        int worldX = chunk.X * 16 + x;
                        int worldZ = chunk.Z * 16 + z;
                        if (IsComparator(block.Identifier) && dimension.GetBlockActor(chunk, x, y, z) is ComparatorBlockActor comparator)
                            comparatorPowers[new Position(worldX, y, worldZ)] = (byte)comparator.OutputSignal;
                    }
                }
            }
        }

        for (int offsetX = -1; offsetX <= 1; offsetX++) {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                Chunk? actorChunk = (offsetX, offsetZ) == (0, 0)
                    ? chunk
                    : dimension.GetChunk(chunk.X + offsetX, chunk.Z + offsetZ);
                if (actorChunk?.blockActors is not { } actors)
                    continue;

                foreach (((int X, int Y, int Z) position, CompoundTag actor) in actors) {
                    if (actor.Get<StringTag>("id")?.Value == "PistonArm")
                        dimension.ScheduleBlockUpdate(actorChunk.X * 16 + position.X, position.Y, actorChunk.Z * 16 + position.Z, 1);
                }
            }
        }

        if (!pistonsWaitingByChunk.Remove((chunk.X, chunk.Z), out HashSet<Position>? waitingPistons))
            return;

        foreach (Position piston in waitingPistons) {
            pistonWaitChunks.Remove(piston);
            dimension.ScheduleBlockUpdate(piston.X, piston.Y, piston.Z, 1);
        }
    }

    internal void BlockChanged(Dimension dimension, int x, int y, int z, BlockPermutation previous, BlockPermutation current) {
        bool fluidChange = previous.Identifier is
            "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava" ||
            current.Identifier is
            "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava";
        bool nonSolidFluidChange = fluidChange && BlockPalette.Replaceable(previous) && BlockPalette.Replaceable(current);
        if (nonSolidFluidChange && (x & 15) is > 0 and < 15 && (y & 15) is > 0 and < 15 && (z & 15) is > 0 and < 15 &&
            dimension.GetChunk(x >> 4, z >> 4)?.GetSubChunk(y >> 4) is not { HasRedstone: true })
            return;

        using ProfilerZone zone = Profiler.BeginZone("Redstone.BlockChanged");
        if (!nonSolidFluidChange) {
            if (previous.Identifier != "minecraft:redstone_wire" || current.Identifier != "minecraft:redstone_wire") {
                bool previousConnection = IsConnectionDevice(previous.Identifier);
                bool currentConnection = IsConnectionDevice(current.Identifier);
                if ((previous.Identifier == "minecraft:redstone_wire") != (current.Identifier == "minecraft:redstone_wire") ||
                    previousConnection != currentConnection ||
                    previousConnection && currentConnection &&
                        (!Equals(previous.State.Values.GetValueOrDefault("minecraft:cardinal_direction"),
                            current.State.Values.GetValueOrDefault("minecraft:cardinal_direction")) ||
                         !Equals(previous.State.Values.GetValueOrDefault("minecraft:facing_direction"),
                            current.State.Values.GetValueOrDefault("minecraft:facing_direction"))) ||
                    previous.Identifier != current.Identifier &&
                        (BlockPalette.FindType(previous.Identifier)?.Solid ?? false) != (BlockPalette.FindType(current.Identifier)?.Solid ?? false))
                    MarkTopologyDirty(x, y, z);
                if (PowerChanged(previous, current)) {
                    MarkPowerChanged(dimension, x, y, z);
                }
            }
        }

        if (chunkLoadRebuild.Contains(new Position(x, y, z)))
            return;

        foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
            int observerX = x + offsetX;
            int observerY = y + offsetY;
            int observerZ = z + offsetZ;
            BlockPermutation observer = dimension.GetPermutation(observerX, observerY, observerZ);
            if (observer.Identifier != "minecraft:observer" ||
                observer.State.Values.GetValueOrDefault("powered_bit") is true ||
                observer.State.Values.GetValueOrDefault("minecraft:facing_direction") is not string direction ||
                !BlockDirections.OffsetByDirection.TryGetValue(direction, out var facing) ||
                (facing.X, facing.Y, facing.Z) != (-offsetX, -offsetY, -offsetZ))
                continue;

            ulong delay = (dimension.TickValue & 1) == 0 ? 1UL : 2UL;
            dimension.ScheduleBlockUpdate(observerX, observerY, observerZ, delay);
        }
    }

    internal void MarkTopologyDirty(int x, int y, int z) {
        for (int offsetX = -1; offsetX <= 1; offsetX++) {
            for (int offsetY = -1; offsetY <= 1; offsetY++) {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                    Position position = new(x + offsetX, y + offsetY, z + offsetZ);
                    if (!components.TryGetValue(position, out WireNode wire))
                        continue;

                    invalidComponents.Add(wire.Component);
                }
            }
        }

        connectionDirty.Add(new Position(x, y, z));
        connectionDirty.Add(new Position(x + 1, y, z));
        connectionDirty.Add(new Position(x - 1, y, z));
        connectionDirty.Add(new Position(x, y, z + 1));
        connectionDirty.Add(new Position(x, y, z - 1));
        MarkDirty(x, y, z);
    }

    internal bool PowerChanged(BlockPermutation previous, BlockPermutation current) {
        if (previous.Identifier == "minecraft:redstone_wire" || current.Identifier == "minecraft:redstone_wire") {
            return previous.Identifier != current.Identifier;
        }

        if (previous.Identifier == "minecraft:lever" || current.Identifier == "minecraft:lever")
            return previous.Identifier != current.Identifier ||
                previous.State.Values.GetValueOrDefault("open_bit") != current.State.Values.GetValueOrDefault("open_bit");

        if (previous.State.Values.ContainsKey("button_pressed_bit") || current.State.Values.ContainsKey("button_pressed_bit"))
            return previous.Identifier != current.Identifier ||
                previous.State.Values.GetValueOrDefault("button_pressed_bit") != current.State.Values.GetValueOrDefault("button_pressed_bit");

        if (IsTorch(previous.Identifier) || IsTorch(current.Identifier))
            return previous.Identifier != current.Identifier;

        if (previous.Identifier == "minecraft:observer" || current.Identifier == "minecraft:observer")
            return previous.Identifier != current.Identifier ||
                previous.State.Values.GetValueOrDefault("powered_bit") != current.State.Values.GetValueOrDefault("powered_bit");

        if (IsPowerDevice(previous.Identifier) || IsPowerDevice(current.Identifier))
            return previous.Identifier != current.Identifier ||
                previous.State.Values.GetValueOrDefault("redstone_signal") != current.State.Values.GetValueOrDefault("redstone_signal") ||
                previous.State.Values.GetValueOrDefault("output_lit_bit") != current.State.Values.GetValueOrDefault("output_lit_bit");

        if (previous.Identifier != current.Identifier &&
            (BlockPalette.FindType(previous.Identifier)?.Components.RedstoneProducer is not null ||
             BlockPalette.FindType(current.Identifier)?.Components.RedstoneProducer is not null))
            return true;

        return false;
    }

    internal void MarkPowerChanged(Dimension dimension, int x, int y, int z) {
        BlockPermutation source = dimension.GetPermutation(x, y, z);
        if (!IsComparator(source.Identifier))
            comparatorPowers.Remove(new Position(x, y, z));

        foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
            int neighborX = x + offsetX;
            int neighborY = y + offsetY;
            int neighborZ = z + offsetZ;
            BlockPermutation neighbor = dimension.GetPermutation(neighborX, neighborY, neighborZ);
            if (IsWire(neighbor))
                dirty.Add(new Position(neighborX, neighborY, neighborZ));
            if (IsPowerDevice(neighbor.Identifier)) {
                if (neighbor.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
                    ScheduleRepeaterUpdate(dimension, neighborX, neighborY, neighborZ, neighbor);
                else
                    dimension.ScheduleBlockUpdate(neighborX, neighborY, neighborZ, DeviceDelay(neighbor));
            }
        }

        if (IsTorch(source.Identifier) &&
            source.State.Values.GetValueOrDefault("torch_facing_direction") is string direction &&
            BlockDirections.TorchSupportByDirection.TryGetValue(direction, out var torchSupport)) {
            foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
                if ((offsetX, offsetY, offsetZ) == torchSupport)
                    continue;

                int neighborX = x + offsetX;
                int neighborY = y + offsetY;
                int neighborZ = z + offsetZ;
                BlockPermutation neighbor = dimension.GetPermutation(neighborX, neighborY, neighborZ);
                if (IsWire(neighbor))
                    dirty.Add(new Position(neighborX, neighborY, neighborZ));
                if (IsSolid(neighbor))
                    ScheduleAttachedTorches(dimension, x, y, z, neighborX, neighborY, neighborZ, 2);
            }
            return;
        }

        foreach ((int X, int Y, int Z) supportOffset in PowerCheckOffsets) {
            int supportX = x + supportOffset.X;
            int supportY = y + supportOffset.Y;
            int supportZ = z + supportOffset.Z;
            if (BlockPalette.FindType(dimension.GetPermutation(supportX, supportY, supportZ).Identifier)?.Solid != true)
                continue;

            ScheduleAttachedTorches(dimension, x, y, z, supportX, supportY, supportZ, 2);
        }
    }

    internal void MarkWirePowerChanged(Dimension dimension, int x, int y, int z) {
        foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
            int supportX = x + offsetX;
            int supportY = y + offsetY;
            int supportZ = z + offsetZ;
            BlockPermutation neighbor = dimension.GetPermutation(supportX, supportY, supportZ);
            if ((neighbor.Identifier is "minecraft:piston" or "minecraft:sticky_piston") &&
                BlockPalette.OnNeighborUpdate(dimension, supportX, supportY, supportZ)) {
                dimension.BroadcastBlockUpdate(supportX, supportY, supportZ, dimension.GetPermutation(supportX, supportY, supportZ));
                dimension.UpdateNeighbors(supportX, supportY, supportZ);
            }

            if (IsPowerDevice(neighbor.Identifier)) {
                if (neighbor.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
                    ScheduleRepeaterUpdate(dimension, supportX, supportY, supportZ, neighbor);
                else
                    dimension.ScheduleBlockUpdate(supportX, supportY, supportZ, DeviceDelay(neighbor));
            }

            if (!IsSolid(neighbor))
                continue;

            ScheduleAttachedTorches(dimension, x, y, z, supportX, supportY, supportZ, 1);
        }
    }

    internal bool? BlockPowered(Dimension dimension, int x, int y, int z) {
        bool unknown = false;
        foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
            int? power = OutputPower(dimension, x + offsetX, y + offsetY, z + offsetZ, x, y, z);
            if (power > 0)
                return true;
            unknown |= power is null;
        }

        return unknown ? null : false;
    }

    internal int? InputPower(Dimension dimension, int x, int y, int z, int targetX, int targetY, int targetZ) {
        BlockPermutation? input = dimension.GetLoadedPermutation(x, y, z);
        if (input is null)
            return null;
        if (IsWire(input) && input.State.Values.GetValueOrDefault("redstone_signal") is int signal)
            return signal;

        int? directPower = OutputPower(dimension, x, y, z, targetX, targetY, targetZ);
        if (directPower is null || directPower > 0)
            return directPower;

        int power = 0;
        bool unknown = false;
        foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
            int sourceX = x + offsetX;
            int sourceY = y + offsetY;
            int sourceZ = z + offsetZ;
            int? sourcePower = OutputPower(dimension, sourceX, sourceY, sourceZ, x, y, z);
            if (sourcePower is null) {
                unknown = true;
                continue;
            }
            power = Math.Max(power, sourcePower.Value);
            if (power == 15)
                return power;
        }

        return unknown ? null : power;
    }

    internal int? ComparatorSidePower(Dimension dimension, int x, int y, int z, (int X, int Y, int Z) back) {
        (int X, int Z)[] sides = back.X != 0 ? ComparatorSidesForX : ComparatorSidesForZ;
        int power = 0;
        bool unknown = false;
        foreach ((int offsetX, int offsetZ) in sides) {
            int? sidePower = InputPower(dimension, x + offsetX, y, z + offsetZ, x, y, z);
            if (sidePower is null) {
                unknown = true;
                continue;
            }
            power = Math.Max(power, sidePower.Value);
            if (power == 15)
                return 15;
        }
        return unknown ? null : power;
    }

    internal bool? RepeaterLocked(Dimension dimension, int x, int y, int z, (int X, int Y, int Z) back) {
        (int X, int Z)[] sides = back.X != 0 ? ComparatorSidesForX : ComparatorSidesForZ;
        bool unknown = false;
        foreach ((int offsetX, int offsetZ) in sides) {
            int sideX = x + offsetX;
            int sideZ = z + offsetZ;
            int? power = OutputPower(dimension, sideX, y, sideZ, x, y, z);
            BlockPermutation? side = dimension.GetLoadedPermutation(sideX, y, sideZ);
            if (side is null) {
                unknown = true;
                continue;
            }
            if (power > 0 && side.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater")
                return true;
        }

        return unknown ? null : false;
    }

    internal void SetComparatorPower(Dimension dimension, int x, int y, int z, int power) {
        Position position = new(x, y, z);
        byte updated = (byte)Math.Clamp(power, 0, 15);
        Chunk? chunk = dimension.GetChunk(x >> 4, z >> 4);
        if (chunk is not null) {
            ComparatorBlockActor? actor = dimension.GetBlockActor(chunk, x & 15, y, z & 15) as ComparatorBlockActor;
            actor ??= dimension.PlaceBlockActor(x, y, z, dimension.GetPermutation(x, y, z).Identifier, null, 0) as ComparatorBlockActor;
            actor?.SetOutputSignal(updated);
        }
        if (comparatorPowers.GetValueOrDefault(position) == updated)
            return;

        comparatorPowers[position] = updated;
        MarkPowerChanged(dimension, x, y, z);
    }

    internal void Update(Dimension dimension) {
        if (dirty.Count == 0 && connectionDirty.Count == 0 && dirtyComponents.Count == 0 &&
            invalidComponents.Count == 0 && updates.Count == 0)
            return;

        foreach (Component component in invalidComponents) {
            dirtyComponents.Remove(component);
            foreach (Position wire in component.Wires) {
                components.Remove(wire);
                sourcePowers.Remove(wire);
                dirty.Add(wire);
            }
        }
        invalidComponents.Clear();

        foreach (Position position in connectionDirty) {
            BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
            if (!IsWire(current))
                continue;

            connectionChanges.Values.Clear();
            if (GetConnection(dimension, position.X, position.Y, position.Z, 1, 0) is { } east)
                connectionChanges.Values["redstone_east"] = east;
            if (GetConnection(dimension, position.X, position.Y, position.Z, 0, -1) is { } north)
                connectionChanges.Values["redstone_north"] = north;
            if (GetConnection(dimension, position.X, position.Y, position.Z, 0, 1) is { } south)
                connectionChanges.Values["redstone_south"] = south;
            if (GetConnection(dimension, position.X, position.Y, position.Z, -1, 0) is { } west)
                connectionChanges.Values["redstone_west"] = west;
            if (connectionChanges.Values.Count == 0)
                continue;
            if (BlockPalette.Find(current.Identifier, current.State, connectionChanges) is not { } connected || connected == current)
                continue;

            dimension.SetPermutation(position.X, position.Y, position.Z, connected);
            QueueUpdate(position.X, position.Y, position.Z, connected);
        }
        connectionDirty.Clear();

        foreach (Position root in dirty) {
            if (components.TryGetValue(root, out WireNode wire)) {
                dirtyComponents.Add(wire.Component);
                wire.Component.DirtySources.Add(wire.Index);
            } else {
                foreach ((int offsetX, int offsetY, int offsetZ) in NeighborOffsets) {
                    Position neighbor = new(root.X + offsetX, root.Y + offsetY, root.Z + offsetZ);
                    if (!components.TryGetValue(neighbor, out wire))
                        continue;

                    dirtyComponents.Add(wire.Component);
                    wire.Component.DirtySources.Add(wire.Index);
                }
            }
            if (components.ContainsKey(root) || !IsWire(dimension.GetPermutation(root.X, root.Y, root.Z)))
                continue;

            visited.Clear();
            wires.Clear();
            unknownBoundaryWires.Clear();
            wiresToVisit.Enqueue(root);
            while (wiresToVisit.TryDequeue(out Position position)) {
                if (!visited.Add(position) || !IsWire(dimension.GetPermutation(position.X, position.Y, position.Z)))
                    continue;

                wires.Add(position);
                bool boundary = false;
                foreach ((int X, int Z) offset in HorizontalOffsets) {
                    int neighborX = position.X + offset.X;
                    int neighborZ = position.Z + offset.Z;
                    if (dimension.GetChunk(neighborX >> 4, neighborZ >> 4) is null) {
                        boundary = true;
                        continue;
                    }

                    BlockPermutation neighbor = dimension.GetPermutation(neighborX, position.Y, neighborZ);
                    bool solid = IsSolid(neighbor);
                    if (IsWire(neighbor))
                        wiresToVisit.Enqueue(new Position(neighborX, position.Y, neighborZ));

                    if (solid && IsWire(dimension.GetPermutation(neighborX, position.Y + 1, neighborZ)))
                        wiresToVisit.Enqueue(new Position(neighborX, position.Y + 1, neighborZ));

                    if (!solid && IsWire(dimension.GetPermutation(neighborX, position.Y - 1, neighborZ)))
                        wiresToVisit.Enqueue(new Position(neighborX, position.Y - 1, neighborZ));
                }

                if (boundary)
                    unknownBoundaryWires.Add(position);
            }

            Position[] positions = wires.ToArray();
            bool rebuildingAfterLoad = false;
            wireIndices.Clear();
            for (int index = 0; index < positions.Length; index++) {
                wireIndices.Add(positions[index], index);
                rebuildingAfterLoad |= chunkLoadRebuild.Contains(positions[index]);
            }
            if (rebuildingAfterLoad) {
                foreach (Position position in positions)
                    chunkLoadRebuild.Add(position);
            }

            int[] neighbors = new int[positions.Length * 8];
            Array.Fill(neighbors, -1);
            for (int index = 0; index < positions.Length; index++) {
                Position position = positions[index];
                int neighborIndex = index * 8;
                for (int direction = 0; direction < HorizontalOffsets.Length; direction++) {
                    (int X, int Z) offset = HorizontalOffsets[direction];
                    int neighborX = position.X + offset.X;
                    int neighborZ = position.Z + offset.Z;
                    if (wireIndices.TryGetValue(new Position(neighborX, position.Y, neighborZ), out int sameLevel))
                        neighbors[neighborIndex + direction * 2] = sameLevel;

                    bool solid = IsSolid(dimension.GetPermutation(neighborX, position.Y, neighborZ));
                    Position stepped = new(neighborX, position.Y + (solid ? 1 : -1), neighborZ);
                    if (wireIndices.TryGetValue(stepped, out int stepLevel))
                        neighbors[neighborIndex + direction * 2 + 1] = stepLevel;
                }
            }

            List<(int Index, byte Power)> boundaryPower = [];
            foreach (Position position in unknownBoundaryWires) {
                if (!wireIndices.TryGetValue(position, out int index))
                    continue;

                BlockPermutation boundaryWire = dimension.GetPermutation(position.X, position.Y, position.Z);
                if (boundaryWire.State.Values.GetValueOrDefault("redstone_signal") is int signal && signal > 0)
                    boundaryPower.Add((index, (byte)Math.Min(signal, 15)));
            }

            Component component = new(positions, neighbors, boundaryPower.ToArray());
            for (int index = 0; index < positions.Length; index++) {
                if (!sourcePowers.TryGetValue(positions[index], out byte sourcePower)) {
                    sourcePower = (byte)GetSourcePower(dimension, positions[index]);
                    sourcePowers.Add(positions[index], sourcePower);
                }
                component.SourcePower[index] = sourcePower;
                if (component.SourcePower[index] > 0)
                    component.Sources.Add(index);

                BlockPermutation permutation = dimension.GetPermutation(positions[index].X, positions[index].Y, positions[index].Z);
                if (permutation.State.Values.GetValueOrDefault("redstone_signal") is int signal && signal > 0) {
                    component.Levels[index] = (byte)signal;
                    component.Active.Add(index);
                }
            }
            for (int index = 0; index < component.Wires.Length; index++)
                components[component.Wires[index]] = new WireNode(component, index);
            dirtyComponents.Add(component);
        }
        dirty.Clear();

        foreach (Component component in dirtyComponents) {
            Position[] wires = component.Wires;
            byte[] levels = component.Levels;
            foreach (int index in component.Active) {
                levels[index] = 0;
                AddCandidate(index);
            }
            component.NextActive.Clear();
            foreach (int index in component.DirtySources) {
                byte previous = component.SourcePower[index];
                byte current = (byte)GetSourcePower(dimension, wires[index]);
                component.SourcePower[index] = current;
                sourcePowers[wires[index]] = current;
                if (previous == current)
                    continue;

                if (previous > 0)
                    component.Sources.Remove(index);
                if (current > 0)
                    component.Sources.Add(index);
            }
            component.DirtySources.Clear();
            foreach (Queue<int> bucket in powerBuckets)
                bucket.Clear();
            foreach (int index in component.Sources) {
                int power = component.SourcePower[index];
                if (levels[index] >= power)
                    continue;

                levels[index] = (byte)power;
                AddCandidate(index);
                component.NextActive.Add(index);
                powerBuckets[power].Enqueue(index);
            }
            foreach ((int index, byte boundaryPower) in component.BoundaryPower) {
                int power = boundaryPower;
                if (levels[index] >= power)
                    continue;

                bool wasUnpowered = levels[index] == 0;
                levels[index] = (byte)power;
                AddCandidate(index);
                if (wasUnpowered)
                    component.NextActive.Add(index);
                powerBuckets[power].Enqueue(index);
            }

            for (int power = 15; power > 1; power--) {
                Queue<int> bucket = powerBuckets[power];
                while (bucket.TryDequeue(out int index)) {
                    if (levels[index] != power)
                        continue;

                    int nextPower = power - 1;
                    int neighborStart = index * 8;
                    for (int neighborOffset = 0; neighborOffset < 8; neighborOffset++) {
                        int neighbor = component.Neighbors[neighborStart + neighborOffset];
                        if (neighbor < 0)
                            continue;

                        if (levels[neighbor] >= nextPower)
                            continue;

                        bool wasUnpowered = levels[neighbor] == 0;
                        levels[neighbor] = (byte)nextPower;
                        AddCandidate(neighbor);
                        if (wasUnpowered)
                            component.NextActive.Add(neighbor);
                        powerBuckets[nextPower].Enqueue(neighbor);
                    }
                }
            }

            foreach (int index in component.Candidates) {
                Position position = wires[index];
                BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
                int currentPower = current.State.Values.GetValueOrDefault("redstone_signal") is int signal ? signal : 0;
                int nextPower = levels[index];
                if (currentPower == nextPower || BlockPalette.Find(current.Identifier, current.State, "redstone_signal", nextPower) is not { } updated)
                    continue;

                dimension.SetPermutation(position.X, position.Y, position.Z, updated);
                QueueUpdate(position.X, position.Y, position.Z, updated);
                MarkWirePowerChanged(dimension, position.X, position.Y, position.Z);
            }

            List<int> previousActive = component.Active;
            component.Active = component.NextActive;
            component.NextActive = previousActive;
            component.NextActive.Clear();
            foreach (int index in component.Candidates) {
                component.CandidateFlags[index] = false;
            }
            component.Candidates.Clear();

            void AddCandidate(int index) {
                if (component.CandidateFlags[index])
                    return;

                component.CandidateFlags[index] = true;
                component.Candidates.Add(index);
            }
        }

        dirtyComponents.Clear();

        foreach ((var position, RedstoneUpdateBatch batch) in updates) {
            UpdateSubChunkBlocksPacket packet = batch.Packet;
            packet.Position.X = position.X << 4;
            packet.Position.Y = position.Y << 4;
            packet.Position.Z = position.Z << 4;
            if (packet.BlocksChanged.Standards.Length != batch.Count)
                packet.BlocksChanged.Standards = new UpdateSubChunkNetworkBlockInfo[batch.Count];
            int index = 0;
            foreach (UpdateSubChunkNetworkBlockInfo block in batch.Pending.Values)
                packet.BlocksChanged.Standards[index++] = block;

            dimension.Broadcast(packet, position.X, position.Z);
            batch.Pending.Clear();
            batch.Count = 0;
            reusableUpdates.Push(batch);
        }
        updates.Clear();
        chunkLoadRebuild.Clear();
    }

    internal void QueueUpdate(int x, int y, int z, BlockPermutation permutation) {
        var subChunk = (X: x >> 4, Y: y >> 4, Z: z >> 4);
        if (!updates.TryGetValue(subChunk, out RedstoneUpdateBatch? batch)) {
            batch = reusableUpdates.TryPop(out RedstoneUpdateBatch? reusable) ? reusable : new RedstoneUpdateBatch();
            updates.Add(subChunk, batch);
        }

        int localIndex = BlockStorage.GetIndex(x & 15, y & 15, z & 15);
        if (batch.Pending.TryGetValue(localIndex, out UpdateSubChunkNetworkBlockInfo? block)) {
            block.RuntimeId = unchecked((uint)permutation.Hash);
            block.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
            return;
        }

        if (batch.Count == batch.Blocks.Count)
            batch.Blocks.Add(new UpdateSubChunkNetworkBlockInfo());
        UpdateSubChunkNetworkBlockInfo update = batch.Blocks[batch.Count];
        update.Position.X = x;
        update.Position.Y = y;
        update.Position.Z = z;
        update.RuntimeId = unchecked((uint)permutation.Hash);
        update.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
        batch.Pending.Add(localIndex, update);
        batch.Count++;
    }

    int GetSourcePower(Dimension dimension, Position position) {
        int power = 0;
        foreach ((int X, int Y, int Z) offset in NeighborOffsets) {
            int sourceX = position.X + offset.X;
            int sourceY = position.Y + offset.Y;
            int sourceZ = position.Z + offset.Z;
            if (IsWire(dimension.GetPermutation(sourceX, sourceY, sourceZ)))
                continue;

            int componentPower = OutputPower(dimension, sourceX, sourceY, sourceZ, position.X, position.Y, position.Z) ?? 0;
            if (componentPower > power)
                power = componentPower;
            if (power >= 15)
                return 15;
        }

        return Math.Clamp(power, 0, 15);
    }

    void ScheduleAttachedTorches(
        Dimension dimension,
        int sourceX,
        int sourceY,
        int sourceZ,
        int supportX,
        int supportY,
        int supportZ,
        ulong delay
    ) {
        foreach ((int X, int Y, int Z) offset in NeighborOffsets) {
            int torchX = supportX + offset.X;
            int torchY = supportY + offset.Y;
            int torchZ = supportZ + offset.Z;
            if (torchX == sourceX && torchY == sourceY && torchZ == sourceZ)
                continue;

            BlockPermutation torch = dimension.GetPermutation(torchX, torchY, torchZ);
            if (!IsTorch(torch.Identifier) ||
                torch.State.Values.GetValueOrDefault("torch_facing_direction") is not string direction ||
                !BlockDirections.TorchSupportByDirection.TryGetValue(direction, out var torchSupport) ||
                (torchSupport.X, torchSupport.Y, torchSupport.Z) != (-offset.X, -offset.Y, -offset.Z))
                continue;

            dimension.ScheduleBlockUpdate(torchX, torchY, torchZ, delay);
        }
    }

    static bool IsTorch(string identifier) => identifier is "minecraft:redstone_torch" or "minecraft:unlit_redstone_torch";

    static bool IsComparator(string identifier) => identifier is "minecraft:powered_comparator" or "minecraft:unpowered_comparator";

    static bool IsPowerDevice(string identifier) => IsComparator(identifier) ||
        identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater" or
        "minecraft:daylight_detector" or "minecraft:daylight_detector_inverted";

    static bool IsNeighborDependentDevice(string identifier) => IsComparator(identifier) || IsTorch(identifier) ||
        identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater";

    static bool IsBoundaryReactive(BlockPermutation permutation) => permutation.Identifier == "minecraft:redstone_wire" ||
        IsNeighborDependentDevice(permutation.Identifier) || permutation.Identifier == "minecraft:lever" ||
        permutation.State.Values.ContainsKey("button_pressed_bit");

    internal static bool IsConnectionDevice(string identifier) => identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater" or
        "minecraft:powered_comparator" or "minecraft:unpowered_comparator" or "minecraft:observer";

    static ulong DeviceDelay(BlockPermutation permutation) => permutation.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater"
        ? (ulong)((permutation.State.Values.GetValueOrDefault("repeater_delay") is int delay ? delay : 0) + 1) * 2
        : 2;

    internal void ScheduleRepeaterUpdate(Dimension dimension, int x, int y, int z, BlockPermutation repeater) {
        if (repeater.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string inputDirection ||
            !BlockDirections.OffsetByCardinalDirection.TryGetValue(inputDirection, out var input))
            return;

        int? inputPower = InputPower(
            dimension,
            x + input.X,
            y + input.Y,
            z + input.Z,
            x,
            y,
            z
        );
        bool? locked = RepeaterLocked(dimension, x, y, z, input);
        if (inputPower is null || locked is null)
            return;

        string target = inputPower > 0 && !locked.Value
            ? "minecraft:powered_repeater"
            : "minecraft:unpowered_repeater";
        if (BlockPalette.Find(target, repeater.State) is not { } scheduled)
            return;

        dimension.ScheduleBlockUpdate(x, y, z, DeviceDelay(repeater), scheduled);
    }

    int? OutputPower(Dimension dimension, int x, int y, int z, int targetX, int targetY, int targetZ) {
        BlockPermutation? source = dimension.GetLoadedPermutation(x, y, z);
        if (source is null)
            return null;
        if (source.Identifier == "minecraft:lever")
            return source.State.Values.GetValueOrDefault("open_bit") is true ? 15 : 0;
        if (source.State.Values.GetValueOrDefault("button_pressed_bit") is true)
            return 15;
        if (IsWire(source))
            return source.State.Values.GetValueOrDefault("redstone_signal") is int wirePower ? wirePower : 0;
        if (source.Identifier is "minecraft:daylight_detector" or "minecraft:daylight_detector_inverted")
            return source.State.Values.GetValueOrDefault("redstone_signal") is int detectorPower ? detectorPower : 0;
        if (source.Identifier == "minecraft:redstone_torch") {
            if (source.State.Values.GetValueOrDefault("torch_facing_direction") is not string torchDirection ||
                !BlockDirections.TorchSupportByDirection.TryGetValue(torchDirection, out var torchSupport) ||
                (x + torchSupport.X, y + torchSupport.Y, z + torchSupport.Z) == (targetX, targetY, targetZ))
                return 0;
            return 15;
        }
        if (source.Identifier == "minecraft:powered_repeater") {
            if (source.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string inputDirection ||
                !BlockDirections.OffsetByCardinalDirection.TryGetValue(inputDirection, out var input) ||
                (x - input.X, y - input.Y, z - input.Z) != (targetX, targetY, targetZ))
                return 0;
            return 15;
        }
        if (source.Identifier == "minecraft:observer") {
            if (source.State.Values.GetValueOrDefault("powered_bit") is not true ||
                source.State.Values.GetValueOrDefault("minecraft:facing_direction") is not string observerDirection ||
                !BlockDirections.OffsetByDirection.TryGetValue(observerDirection, out var facing) ||
                (x - facing.X, y - facing.Y, z - facing.Z) != (targetX, targetY, targetZ))
                return 0;
            return 15;
        }
        if (IsComparator(source.Identifier)) {
            if (!comparatorPowers.TryGetValue(new Position(x, y, z), out byte comparatorPower) ||
                source.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string comparatorBackDirection ||
                !BlockDirections.OffsetByCardinalDirection.TryGetValue(comparatorBackDirection, out var comparatorBack) ||
                (x - comparatorBack.X, y - comparatorBack.Y, z - comparatorBack.Z) != (targetX, targetY, targetZ))
                return 0;
            return comparatorPower;
        }

        return BlockPalette.FindType(source.Identifier)?.Components.RedstoneProducer?.Power ?? 0;
    }

    static bool IsWire(BlockPermutation permutation) => permutation.Identifier == "minecraft:redstone_wire";

    static bool IsSolid(BlockPermutation permutation) => BlockPalette.FindType(permutation.Identifier)?.Solid == true;

    static string? GetConnection(Dimension dimension, int x, int y, int z, int offsetX, int offsetZ) {
        int neighborX = x + offsetX;
        int neighborZ = z + offsetZ;
        if (dimension.GetChunk(neighborX >> 4, neighborZ >> 4) is null)
            return null;

        BlockPermutation neighbor = dimension.GetPermutation(neighborX, y, neighborZ);
        if (IsWire(neighbor))
            return "side";
        if (neighbor.Identifier is "minecraft:powered_repeater" or "minecraft:unpowered_repeater" or
            "minecraft:powered_comparator" or "minecraft:unpowered_comparator") {
            if (neighbor.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is string direction &&
                BlockDirections.OffsetByCardinalDirection.TryGetValue(direction, out var back) &&
                ((offsetX != 0 && back.X != 0) || (offsetZ != 0 && back.Z != 0)))
                return "side";
        }
        if (neighbor.Identifier == "minecraft:observer" &&
            neighbor.State.Values.GetValueOrDefault("minecraft:facing_direction") is string observerDirection &&
            BlockDirections.OffsetByDirection.TryGetValue(observerDirection, out var observerFacing) &&
            (observerFacing.X, observerFacing.Y, observerFacing.Z) == (offsetX, 0, offsetZ))
            return "side";

        if (IsSolid(neighbor))
            return IsWire(dimension.GetPermutation(neighborX, y + 1, neighborZ)) ? "up" : "none";

        return IsWire(dimension.GetPermutation(neighborX, y - 1, neighborZ)) ? "side" : "none";
    }

    static readonly (int X, int Z)[] HorizontalOffsets = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    static readonly (int X, int Y, int Z)[] NeighborOffsets = [
        (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)
    ];
    static readonly (int X, int Y, int Z)[] PowerCheckOffsets = [
        (0, 0, 0), (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)
    ];
    static readonly (int X, int Z)[] ComparatorSidesForX = [(0, 1), (0, -1)];
    static readonly (int X, int Z)[] ComparatorSidesForZ = [(1, 0), (-1, 0)];

    readonly record struct Position(int X, int Y, int Z);

    readonly record struct WireNode(Component Component, int Index);

    sealed class RedstoneUpdateBatch {
        internal readonly Dictionary<int, UpdateSubChunkNetworkBlockInfo> Pending = [];
        internal readonly List<UpdateSubChunkNetworkBlockInfo> Blocks = [];
        internal readonly UpdateSubChunkBlocksPacket Packet = new();
        internal int Count;
    }

    sealed class Component(Position[] wires, int[] neighbors, (int Index, byte Power)[] boundaryPower) {
        internal Position[] Wires { get; } = wires;
        internal int[] Neighbors { get; } = neighbors;
        internal (int Index, byte Power)[] BoundaryPower { get; } = boundaryPower;
        internal byte[] Levels { get; } = new byte[wires.Length];
        internal byte[] SourcePower { get; } = new byte[wires.Length];
        internal HashSet<int> Sources { get; } = [];
        internal HashSet<int> DirtySources { get; } = [];
        internal bool[] CandidateFlags { get; } = new bool[wires.Length];
        internal List<int> Candidates { get; } = [];
        internal List<int> Active = [];
        internal List<int> NextActive = [];
    }
}
