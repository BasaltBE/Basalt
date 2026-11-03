using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:bamboo", "minecraft:bamboo_sapling")]
public sealed class BambooTrait : BlockSupportTrait {
    static readonly BlockPermutation Sapling = BlockPalette.FindDefaultPermutation("minecraft:bamboo_sapling")!;
    internal override bool HasRandomTicks => true;

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        if (y <= dimension.MinimumY)
            return false;

        BlockPermutation below = dimension.GetPermutation(x, y - 1, z);
        return below.Identifier is "minecraft:bamboo" or "minecraft:bamboo_sapling" ||
            BlockPalette.FindType(below.Identifier)?.Tags.Any(static tag => tag is "dirt" or "sand" or "gravel") == true;
    }

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (HasSupport(player.Dimension, x, y, z) != true)
            return false;

        BlockPermutation below = player.Dimension.GetPermutation(x, y - 1, z);
        if (below.Identifier is not ("minecraft:bamboo" or "minecraft:bamboo_sapling")) {
            permutation = Sapling;
            return true;
        }

        bool thick = below.Identifier == "minecraft:bamboo" &&
            (below.State.Values.GetValueOrDefault("age_bit") is true ||
             y > player.Dimension.MinimumY && player.Dimension.GetPermutation(x, y - 2, z).Identifier == "minecraft:bamboo");
        permutation = FindBamboo(thick, "no_leaves") ?? permutation;
        return true;
    }

    public override bool OnUseItemOnBlock(
        Player player,
        ItemStack? item,
        BlockPermutation block,
        BlockPos position,
        byte face,
        Vec3 clickPosition
    ) {
        if (item?.Type.Identifier != "minecraft:bone_meal" ||
            block.Identifier is not ("minecraft:bamboo" or "minecraft:bamboo_sapling"))
            return false;

        int topY = position.Y;
        while (topY < player.Dimension.MaximumY &&
            player.Dimension.GetPermutation(position.X, topY + 1, position.Z).Identifier == "minecraft:bamboo")
            topY++;

        int height = 1;
        int bottomY = topY;
        while (bottomY > player.Dimension.MinimumY &&
            player.Dimension.GetPermutation(position.X, bottomY - 1, position.Z).Identifier is "minecraft:bamboo" or "minecraft:bamboo_sapling") {
            bottomY--;
            height++;
        }

        if (height >= 16 || topY >= player.Dimension.MaximumY ||
            player.Dimension.GetPermutation(position.X, topY + 1, position.Z).Identifier is not (
                "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return false;

        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.UseOnBlock, 0);
            player.Network.Server.Events.Emit(itemUse);
            if (itemUse.IsCanceled())
                return true;
        }

        if (block.Identifier == "minecraft:bamboo_sapling" && topY == position.Y) {
            if (FindBamboo(false, "no_leaves") is not { } stem ||
                FindBamboo(false, "small_leaves") is not { } leaves ||
                !player.Dimension.SetPermutation(position.X, position.Y, position.Z, stem))
                return false;

            if (!player.Dimension.SetPermutation(position.X, position.Y + 1, position.Z, leaves)) {
                player.Dimension.SetPermutation(position.X, position.Y, position.Z, block);
                return false;
            }

            topY++;
            height++;
            player.Dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, stem);
            player.Dimension.BroadcastBlockUpdate(position.X, topY, position.Z, leaves);
            player.Dimension.UpdateNeighbors(position.X, position.Y, position.Z);
            player.Dimension.UpdateNeighbors(position.X, topY, position.Z);
        }

        int growth = Math.Min(Random.Shared.Next(1, 3), 16 - height);
        for (int index = 0; index < growth; index++) {
            if (!Grow(player.Dimension, position.X, topY, position.Z))
                break;
            topY++;
        }

        if (topY == position.Y)
            return false;

        Vec3 center = new() { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f };
        player.Dimension.Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.ParticlesCropGrowth,
            Position = center,
            Data = 15
        }, center);
        player.Dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "item.bone_meal.use",
            Position = center,
            ActorUniqueId = -1
        }, center);

        if (!player.Abilities.Get(PlayerAbility.InstantBuild))
            item.DecrementStack();
        return true;
    }

    public override void OnRandomTick(Dimension dimension, BlockPermutation block, int x, int y, int z) {
        if (y >= dimension.MaximumY || Random.Shared.Next(3) != 0 ||
            dimension.GetPermutation(x, y + 1, z).Identifier is not (
                "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return;

        int height = 1;
        int bottomY = y;
        while (bottomY > dimension.MinimumY &&
            dimension.GetPermutation(x, bottomY - 1, z).Identifier is "minecraft:bamboo" or "minecraft:bamboo_sapling") {
            bottomY--;
            height++;
        }
        int topY = bottomY + height - 1;
        while (topY < dimension.MaximumY &&
            dimension.GetPermutation(x, topY + 1, z).Identifier == "minecraft:bamboo") {
            topY++;
            height++;
        }

        if (height < 16) {
            Grow(dimension, x, topY, z);
        }
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation block = dimension.GetPermutation(x, y, z);
        if (HasSupport(dimension, x, y, z) is false)
            return base.OnNeighborUpdate(dimension, x, y, z);

        if (block.Identifier == "minecraft:bamboo_sapling" && y < dimension.MaximumY &&
            dimension.GetPermutation(x, y + 1, z).Identifier == "minecraft:bamboo" &&
            FindBamboo(false, "no_leaves") is { } bamboo)
            return dimension.SetPermutation(x, y, z, bamboo);

        return base.OnNeighborUpdate(dimension, x, y, z);
    }

    protected override void OnSupportLost(Dimension dimension, int x, int y, int z, bool dropItem) {
        base.OnSupportLost(dimension, x, y, z, dropItem);
        dimension.UpdateNeighbors(x, y, z);
    }

    bool Grow(Dimension dimension, int x, int y, int z) {
        if (y >= dimension.MaximumY || dimension.GetPermutation(x, y + 1, z).Identifier is not (
            "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return false;

        BlockPermutation top = dimension.GetPermutation(x, y, z);
        BlockPermutation? newTop;
        if (top.Identifier == "minecraft:bamboo_sapling") {
            if (FindBamboo(false, "no_leaves") is not { } baseBamboo ||
                FindBamboo(false, "small_leaves") is not { } leaves)
                return false;

            newTop = leaves;
            if (!dimension.SetPermutation(x, y, z, baseBamboo))
                return false;
        }
        else {
            string leaves = top.State.Values.GetValueOrDefault("bamboo_leaf_size") as string ?? "no_leaves";
            bool thick = top.State.Values.GetValueOrDefault("age_bit") is true ||
                y - 2 > dimension.MinimumY && dimension.GetPermutation(x, y - 2, z).Identifier == "minecraft:bamboo";
            newTop = FindBamboo(thick, leaves == "no_leaves" ? "small_leaves" : "large_leaves");
            if (leaves != "no_leaves" && FindBamboo(thick, "small_leaves") is { } smallerLeaves)
                dimension.SetPermutation(x, y, z, smallerLeaves);
            if (leaves != "no_leaves" && y - 1 > dimension.MinimumY &&
                dimension.GetPermutation(x, y - 1, z) is { Identifier: "minecraft:bamboo" } lower &&
                FindBamboo(lower.State.Values.GetValueOrDefault("age_bit") is true, "no_leaves") is { } bareStem)
                dimension.SetPermutation(x, y - 1, z, bareStem);
        }

        if (newTop is null || !dimension.SetPermutation(x, y + 1, z, newTop))
            return false;

        dimension.BroadcastBlockUpdate(x, y + 1, z, newTop);
        dimension.BroadcastBlockUpdate(x, y, z, dimension.GetPermutation(x, y, z));
        if (y - 1 > dimension.MinimumY && dimension.GetPermutation(x, y - 1, z).Identifier == "minecraft:bamboo")
            dimension.BroadcastBlockUpdate(x, y - 1, z, dimension.GetPermutation(x, y - 1, z));
        dimension.UpdateNeighbors(x, y, z);
        dimension.UpdateNeighbors(x, y + 1, z);
        return true;
    }

    static BlockPermutation? FindBamboo(bool thick, string leaves) {
        BlockPermutation defaultBamboo = BlockPalette.FindDefaultPermutation("minecraft:bamboo")!;
        BlockState state = new() { Values = new(defaultBamboo.State.Values, StringComparer.Ordinal) };
        state.Values["age_bit"] = thick;
        state.Values["bamboo_leaf_size"] = leaves;
        state.Values["bamboo_stalk_thickness"] = thick ? "thick" : "thin";
        return BlockPalette.Find("minecraft:bamboo", state);
    }
}
