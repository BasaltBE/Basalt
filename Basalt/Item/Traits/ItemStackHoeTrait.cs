namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Item.Traits.Types;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public sealed class ItemStackHoeTrait : ItemTrait {
    public new static string Identifier => "hoe_till";
    public new static readonly string[] Tags = ["minecraft:is_hoe"];

    private static readonly Dictionary<string, string> TillableBlocks = new(StringComparer.Ordinal) {
        [BlockIdentifier.GrassBlock.ToIdentifier()] = BlockIdentifier.Farmland.ToIdentifier(),
        [BlockIdentifier.GrassPath.ToIdentifier()] = BlockIdentifier.Farmland.ToIdentifier(),
        [BlockIdentifier.Dirt.ToIdentifier()] = BlockIdentifier.Farmland.ToIdentifier(),
        [BlockIdentifier.CoarseDirt.ToIdentifier()] = BlockIdentifier.Dirt.ToIdentifier()
    };

    public ItemStackHoeTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        if (details.Player.Dimension is null) {
            return;
        }

        var dimension = details.Player.Dimension;
        BlockPos pos = details.BlockPosition;

        BlockPermutation current = dimension.GetPermutation(
            pos.X,
            pos.Y,
            pos.Z
        );

        if (!TillableBlocks.TryGetValue(
            current.Type.Identifier,
            out string? resultIdentifier
        )) {
            return;
        }

        BlockType? resultType = BlockType.Get(resultIdentifier);
        if (resultType is null) {
            return;
        }

        BlockPermutation resultPermutation = resultType.GetPermutation();

        dimension.SetPermutation(
            pos.X,
            pos.Y,
            pos.Z,
            resultPermutation
        );

        if (
            string.Equals(
                resultIdentifier,
                BlockIdentifier.Farmland.ToIdentifier(),
                StringComparison.Ordinal
            )
        ) {
            Basalt.Core.Blocks.Traits.FarmlandTrait
                .ScheduleFarmlandTick(dimension, pos);
        }

        dimension.Broadcast(new UpdateBlockPacket {
            BlockPosition = pos,
            BlockRuntimeID = (uint)resultPermutation.NetworkId,
            Flags = (uint)UpdateBlockFlagsType.Network,
            Layer = (uint)UpdateBlockLayerType.Normal
        });

        dimension.PlaySound(LevelSoundEvent.item_use_on.ToProtoString(), new Vec3 {
                X = pos.X + 0.5f,
                Y = pos.Y + 0.5f,
                Z = pos.Z + 0.5f
            },
            data: resultPermutation.NetworkId);
    }
}
