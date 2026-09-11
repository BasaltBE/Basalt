namespace Basalt.Core.Blocks.Traits;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Traits.Types;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

public sealed class MovingBlockTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.MovingBlock.ToIdentifier()];

    public MovingBlockTrait(Block block) : base(block) {
    }

    public override void OnTick(BlockTickDetails details) {
        BlockLevelStorage? storage = details.Dimension
            .GetLoadedChunk(details.BlockPosition.X >> 4, details.BlockPosition.Z >> 4)
            ?.GetBlockStorage(details.BlockPosition);
        int networkId = storage?.Get<IntTag>("moving_block_runtime")?.Value ?? 0;
        CompoundTag? originalStorage = storage?.Get<CompoundTag>("moving_block_storage");
        if (networkId == 0) {
            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        BlockPermutation moved = BlockPermutation.Resolve(networkId);
        if (moved.Type.Identifier == BlockIdentifier.MovingBlock.ToIdentifier()) {
            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        details.Dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            moved);

        if (originalStorage is not null) {
            ChunkColumn? targetChunk = details.Dimension.GetLoadedChunk(details.BlockPosition.X >> 4, details.BlockPosition.Z >> 4);
            if (targetChunk is not null) {
                BlockLevelStorage restoredStorage = new(targetChunk, originalStorage);
                targetChunk.SetBlockStorage(details.BlockPosition, restoredStorage);
                details.Dimension.Broadcast(new BlockActorDataPacket {
                    Position = details.BlockPosition,
                    ActorData = restoredStorage
                });
            }
        }
    }
}
