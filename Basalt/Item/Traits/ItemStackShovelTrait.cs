namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Item.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;


public sealed class ItemStackShovelTrait : ItemTrait {
    public new static string Identifier => "shovel_flatten";
    public new static readonly string[] Tags = ["minecraft:is_shovel"];

    private static readonly HashSet<string> FlattenableBlocks = new(StringComparer.Ordinal)
    {
        BlockIdentifier.GrassBlock.ToIdentifier(),
        BlockIdentifier.Mycelium.ToIdentifier(),
        BlockIdentifier.Dirt.ToIdentifier(),
        BlockIdentifier.Farmland.ToIdentifier()
    };

    public ItemStackShovelTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        if (details.Player.Dimension is null) {
            return;
        }

        var dimension = details.Player.Dimension;
        BlockPos pos = details.BlockPosition;

        BlockPermutation current = dimension.GetPermutation(pos.X, pos.Y, pos.Z);

        if (!FlattenableBlocks.Contains(current.Type.Identifier)) {
            return;
        }

        BlockType? grassPath = BlockType.Get(BlockIdentifier.GrassPath.ToIdentifier());
        if (grassPath is null) {
            return;
        }

        BlockPermutation resultPermutation = grassPath.GetPermutation();

        dimension.SetPermutation(pos.X, pos.Y, pos.Z, resultPermutation);

        dimension.Broadcast(new UpdateBlockPacket {
            Position = pos,
            NetworkBlockId = (uint)resultPermutation.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });

        dimension.Broadcast(new LevelSoundEventPacket {
            Event = LevelSoundEvent.ItemUseOn,
            Position = new Vec3f {
                X = pos.X + 0.5f,
                Y = pos.Y + 0.5f,
                Z = pos.Z + 0.5f
            },
            Data = resultPermutation.NetworkId,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        });
    }
}
