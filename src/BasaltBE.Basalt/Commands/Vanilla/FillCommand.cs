using System.Globalization;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class FillCommand : Command {
    const int MaximumBlocks = 32768;

    public FillCommand() : base("fill", "Fills all or parts of a region with a specific block.", "basalt.command.fill") {
        PositionEnum position = new(integer: true);
        BlockEnum block = new();
        BlockStatesEnum states = new(array: true);
        CustomEnum mode = new("FillMode", ["destroy", "hollow", "keep", "outline", "replace"]);

        AddOverload(
            new CommandParameter("from", position),
            new CommandParameter("to", position),
            new CommandParameter("tileName", block)
        );
        AddOverload(
            new CommandParameter("from", position),
            new CommandParameter("to", position),
            new CommandParameter("tileName", block),
            new CommandParameter("oldBlockHandling", mode),
            new CommandParameter("replaceTileName", block, optional: true),
            new CommandParameter("replaceBlockStates", states, optional: true)
        );
        AddOverload(
            new CommandParameter("from", position),
            new CommandParameter("to", position),
            new CommandParameter("tileName", block),
            new CommandParameter("blockStates", states)
        );
        AddOverload(
            new CommandParameter("from", position),
            new CommandParameter("to", position),
            new CommandParameter("tileName", block),
            new CommandParameter("blockStates", states),
            new CommandParameter("oldBlockHandling", mode),
            new CommandParameter("replaceTileName", block, optional: true),
            new CommandParameter("replaceBlockStates", states, optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        Dimension? dimension = context.Player?.Dimension;
        if (dimension is null) {
            foreach (Dimension candidate in context.Server.GetWorld().Dimensions.Values) {
                dimension = candidate;
                break;
            }
        }

        if (dimension is null) return new(false, "No dimension is available.");

        BlockPos from = context.Get<BlockPos>("from");
        BlockPos to = context.Get<BlockPos>("to");
        int minimumX = Math.Min(from.X, to.X);
        int minimumY = Math.Min(from.Y, to.Y);
        int minimumZ = Math.Min(from.Z, to.Z);
        int maximumX = Math.Max(from.X, to.X);
        int maximumY = Math.Max(from.Y, to.Y);
        int maximumZ = Math.Max(from.Z, to.Z);

        if (minimumY < dimension.MinimumY || maximumY > dimension.MaximumY)
            return new(false, "Cannot place blocks outside of the world.");

        if (minimumX < -30_000_000 || maximumX > 30_000_000 || minimumZ < -30_000_000 || maximumZ > 30_000_000)
            return new(false, "Cannot place blocks outside of the world.");

        decimal volume = (maximumX - (decimal)minimumX + 1) * (maximumY - (decimal)minimumY + 1) *
            (maximumZ - (decimal)minimumZ + 1);
        if (volume > MaximumBlocks)
            return new(false, string.Format(CultureInfo.InvariantCulture,
                "Too many blocks in the specified area ({0} > {1}).", volume, MaximumBlocks));

        BlockType blockType = context.Get<BlockType>("tileName");
        BlockPermutation? target = BlockPalette.FindDefaultPermutation(blockType.Identifier);
        if (target is null) return new(false, $"Cannot fill with block '{blockType.Identifier}'.");

        bool hasBlockStates = Array.Exists(context.Parameters, static parameter => parameter.Name == "blockStates");
        if (hasBlockStates && context.Get<BlockState>("blockStates") is { } blockStates) {
            target = BlockPalette.Find(blockType.Identifier, target.State, blockStates);
            if (target is null) return new(false, $"Invalid block states for '{blockType.Identifier}'.");
        }

        string mode = Array.Exists(context.Parameters, static parameter => parameter.Name == "oldBlockHandling")
            ? context.Get<string>("oldBlockHandling")
            : "replace";
        BlockType? filterType = Array.Exists(context.Parameters, static parameter => parameter.Name == "replaceTileName")
            ? context.Get<BlockType>("replaceTileName")
            : null;
        BlockState? filterStates = Array.Exists(context.Parameters, static parameter => parameter.Name == "replaceBlockStates")
            ? context.Get<BlockState>("replaceBlockStates")
            : null;

        if (filterType is not null && mode != "replace")
            return new(false, "A replacement block can only be used with the replace fill mode.");
        if (filterStates is not null && filterType is null)
            return new(false, "A replacement block is required when block states are specified.");
        BlockPermutation? filterDefault = filterType is null ? null : BlockPalette.FindDefaultPermutation(filterType.Identifier);
        if (filterType is not null && filterDefault is null) return new(false, $"Cannot filter by block '{filterType.Identifier}'.");
        if (filterType is not null && filterStates is not null &&
            BlockPalette.Find(filterType.Identifier, filterDefault!.State, filterStates) is null)
            return new(false, $"Invalid replacement block states for '{filterType.Identifier}'.");

        Dictionary<(int X, int Y, int Z), List<UpdateSubChunkNetworkBlockInfo>> updates = [];
        HashSet<(int X, int Y, int Z)> neighbors = [];
        int filled = 0;

        for (long x = minimumX; x <= maximumX; x++) {
            for (long y = minimumY; y <= maximumY; y++) {
                for (long z = minimumZ; z <= maximumZ; z++) {
                    int blockX = (int)x;
                    int blockY = (int)y;
                    int blockZ = (int)z;
                    BlockPermutation current = dimension.GetPermutation(blockX, blockY, blockZ);
                    if ((mode == "keep" && BlockPalette.FindType(current.Identifier)?.Air != true) ||
                        (mode == "replace" && filterType is not null && current.Identifier != filterType.Identifier) ||
                        filterStates is not null && filterStates.Values.Any(state =>
                            !current.State.Values.TryGetValue(state.Key, out object? value) || !Equals(value, state.Value)))
                        continue;

                    bool boundary = x == minimumX || x == maximumX || y == minimumY || y == maximumY || z == minimumZ || z == maximumZ;
                    if (mode == "outline" && !boundary) continue;

                    BlockPermutation replacement = mode == "hollow" && !boundary ? BlockStorage.Air : target;
                    if (current == replacement) continue;

                    Chunk? chunk = dimension.GetChunk(blockX >> 4, blockZ >> 4);
                    BlockActor? actor = chunk is null
                        ? null
                        : dimension.GetBlockActor(chunk, blockX & 15, blockY, blockZ & 15);
                    if (actor is not null && !actor.CanRemove()) continue;

                    ItemStack? actorDrop = null;
                    if (mode == "destroy" && actor is not null) {
                        if (context.Player is { } player) actor.OnBreak(player);
                        actorDrop = actor.CreateDrop();
                    }

                    if (!dimension.SetPermutation(blockX, blockY, blockZ, replacement)) continue;

                    if (mode == "destroy") {
                        if (actor is not null) {
                            if (context.Player is null && actorDrop is null && actor.Inventory is { } inventory) {
                                for (int slot = 0; slot < inventory.ContainerSize; slot++) {
                                    if (inventory.TakeItem(slot, ushort.MaxValue) is { } item)
                                        new ItemEntity(item, blockX, blockY, blockZ).Spawn(dimension);
                                }
                            }

                            if (actorDrop is not null) new ItemEntity(actorDrop, blockX, blockY, blockZ).Spawn(dimension);
                        }

                        if (current.State.Values.GetValueOrDefault("upper_block_bit") is not true) {
                            foreach (BlockDrop drop in context.Server.LootTables.Get(current.Identifier)) {
                                if (ItemPalette.Find(drop.Identifier) is not { } itemType || itemType.NetworkId == 0 ||
                                    itemType.MaxAmount is <= 0 or > ushort.MaxValue)
                                    continue;

                                int amount = drop.MinAmount == drop.MaxAmount
                                    ? drop.MinAmount
                                    : (int)Random.Shared.NextInt64(drop.MinAmount, (long)drop.MaxAmount + 1);
                                while (amount > 0) {
                                    ushort stackSize = (ushort)Math.Min(itemType.MaxAmount, amount);
                                    new ItemEntity(new ItemStack(itemType, stackSize), blockX, blockY, blockZ).Spawn(dimension);
                                    amount -= stackSize;
                                }
                            }
                        }
                    }

                    (int X, int Y, int Z) subChunk = (blockX >> 4, blockY >> 4, blockZ >> 4);
                    if (!updates.TryGetValue(subChunk, out List<UpdateSubChunkNetworkBlockInfo>? blocks)) {
                        blocks = [];
                        updates.Add(subChunk, blocks);
                    }
                    blocks.Add(new UpdateSubChunkNetworkBlockInfo {
                        Position = new BlockPos { X = blockX, Y = blockY, Z = blockZ },
                        RuntimeId = unchecked((uint)replacement.Hash),
                        Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
                    });
                    neighbors.Add((blockX + 1, blockY, blockZ));
                    neighbors.Add((blockX - 1, blockY, blockZ));
                    neighbors.Add((blockX, blockY + 1, blockZ));
                    neighbors.Add((blockX, blockY - 1, blockZ));
                    neighbors.Add((blockX, blockY, blockZ + 1));
                    neighbors.Add((blockX, blockY, blockZ - 1));
                    filled++;
                }
            }
        }

        foreach (((int chunkX, int subChunkY, int chunkZ), List<UpdateSubChunkNetworkBlockInfo> blocks) in updates) {
            dimension.Broadcast(new UpdateSubChunkBlocksPacket {
                Position = new BlockPos { X = chunkX << 4, Y = subChunkY << 4, Z = chunkZ << 4 },
                BlocksChanged = new UpdateSubChunkBlocksChangedInfo { Standards = [.. blocks] }
            }, chunkX, chunkZ);
        }

        foreach ((int x, int y, int z) in neighbors) dimension.UpdateNeighbor(x, y, z);

        return filled == 0
            ? new(false, "No blocks filled")
            : new(true, string.Format(CultureInfo.InvariantCulture, "{0} blocks filled", filled));
    }
}
