using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits.Types;

public abstract class BlockSupportTrait : BlockTrait {
    protected abstract bool? HasSupport(Dimension dimension, int x, int y, int z);

    protected virtual void OnSupportLost(Dimension dimension, int x, int y, int z, bool dropItem) {
        BlockPermutation block = dimension.GetPermutation(x, y, z);
        dimension.SetPermutation(x, y, z, BlockStorage.Air);
        Vec3 position = new() { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f };
        dimension.Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.ParticlesDestroyBlock,
            Position = position,
            Data = block.Hash
        }, position);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "break",
            Position = position,
            Data = block.NetworkId,
            ActorUniqueId = -1
        }, position);

        if (dropItem && dimension.World is { } world) {
            ReadOnlySpan<BlockDrop> drops = world.LootTables.Get(block.Identifier);
            foreach (BlockDrop drop in drops) {
                ItemType? itemType = ItemPalette.Find(drop.Identifier);
                if (itemType is null || itemType.NetworkId == 0 || itemType.MaxAmount is <= 0 or > ushort.MaxValue) continue;

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

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        if (HasSupport(dimension, x, y, z) is not false) return false;

        OnSupportLost(dimension, x, y, z, dropItem: true);
        return true;
    }
}
