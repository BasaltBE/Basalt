namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class ItemStackBucketTrait : ItemTrait {
    public new static string Identifier => "bucket_place";
    public new static readonly string[] Types =
    [
        ItemIdentifier.WaterBucket.ToIdentifier(),
        ItemIdentifier.LavaBucket.ToIdentifier()
    ];

    public ItemStackBucketTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        if (details.Player.Dimension is null) return;

        Dimension dimension = details.Player.Dimension;
        World? world = dimension.World;
        if (world is null || world.TickValue < details.Player.BucketCooldownTick) {
            return;
        }

        BlockPos clickedPos = details.BlockPosition;
        int face = details.BlockFace;

        BlockPos placePos = GetPlacedPosition(clickedPos, face);

        BlockPermutation existing = dimension.GetPermutation(placePos.X, placePos.Y, placePos.Z);
        if (!existing.Type.Air && !existing.Type.Liquid) {
            return;
        }

        FluidKind kind = GetFluidKind();
        BlockPermutation? sourcePerm = FluidTrait.SourcePerm(kind);
        if (sourcePerm is null) return;

        dimension.RemoveBlock(placePos.X, placePos.Y, placePos.Z);
        dimension.SetPermutation(placePos.X, placePos.Y, placePos.Z, sourcePerm, 0, true);
        details.Player.BucketCooldownTick =
            world.TickValue + Player.Player.BucketCooldownTicks;

        FluidTrait.ScheduleFluidTick(dimension, placePos, kind);

        string soundEvent = kind == FluidKind.Water
            ? "bucket.empty_water"
            : "bucket.empty_lava";

        dimension.PlaySound(soundEvent, new Vec3 {
                X = placePos.X + 0.5f,
                Y = placePos.Y + 0.5f,
                Z = placePos.Z + 0.5f
            },
            data: sourcePerm.NetworkId);

        if (details.Player.Gamemode == GameType.Survival) {
            var inventory = details.Player.GetTrait<Basalt.Core.Entities.Traits.EntityInventoryTrait>();
            if (inventory is not null) {
                ItemType? emptyBucket = ItemType.Get(ItemIdentifier.Bucket.ToIdentifier());
                if (emptyBucket is not null) {
                    inventory.Container.SetItem(details.HotBarSlot, new ItemStack(emptyBucket, 1));
                }
            }
        }
    }

    private FluidKind GetFluidKind() {
        if (string.Equals(ItemStack.Identifier, ItemIdentifier.LavaBucket.ToIdentifier(), StringComparison.Ordinal))
            return FluidKind.Lava;
        return FluidKind.Water;
    }

    private static BlockPos GetPlacedPosition(BlockPos clicked, int face) {
        return face switch {
            0 => new BlockPos { X = clicked.X, Y = clicked.Y - 1, Z = clicked.Z }, // Down
            1 => new BlockPos { X = clicked.X, Y = clicked.Y + 1, Z = clicked.Z }, // Up
            2 => new BlockPos { X = clicked.X, Y = clicked.Y, Z = clicked.Z - 1 }, // North
            3 => new BlockPos { X = clicked.X, Y = clicked.Y, Z = clicked.Z + 1 }, // South
            4 => new BlockPos { X = clicked.X - 1, Y = clicked.Y, Z = clicked.Z }, // West
            5 => new BlockPos { X = clicked.X + 1, Y = clicked.Y, Z = clicked.Z }, // East
            _ => new BlockPos { X = clicked.X, Y = clicked.Y + 1, Z = clicked.Z }
        };
    }
}
