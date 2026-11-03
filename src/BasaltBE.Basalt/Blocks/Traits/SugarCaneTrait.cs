using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:reeds")]
public sealed class SugarCaneTrait : BlockSupportTrait {
    static readonly BlockPermutation Reeds = BlockPalette.FindDefaultPermutation("minecraft:reeds")!;
    internal override bool HasRandomTicks => true;

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        if (y <= dimension.MinimumY)
            return false;

        BlockPermutation below = dimension.GetPermutation(x, y - 1, z);
        if (below.Identifier == "minecraft:reeds")
            return true;
        if (BlockPalette.FindType(below.Identifier)?.Tags.Any(static tag => tag is "dirt" or "sand") != true)
            return false;

        for (byte face = 2; face < 6; face++) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            string neighbor = dimension.GetPermutation(x + offsetX, y - 1, z + offsetZ).Identifier;
            if (neighbor is "minecraft:water" or "minecraft:flowing_water")
                return true;
        }

        return false;
    }

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) =>
        HasSupport(player.Dimension, x, y, z) == true;

    public override bool OnUseItemOnBlock(
        Player player,
        ItemStack? item,
        BlockPermutation block,
        BlockPos position,
        byte face,
        Vec3 clickPosition
    ) {
        if (item?.Type.Identifier != "minecraft:bone_meal" || block.Identifier != "minecraft:reeds")
            return false;

        int topY = position.Y;
        while (topY < player.Dimension.MaximumY &&
            player.Dimension.GetPermutation(position.X, topY + 1, position.Z).Identifier == "minecraft:reeds")
            topY++;

        int height = 1;
        int bottomY = topY;
        while (bottomY > player.Dimension.MinimumY &&
            player.Dimension.GetPermutation(position.X, bottomY - 1, position.Z).Identifier == "minecraft:reeds") {
            bottomY--;
            height++;
        }

        if (height >= 3 || topY >= player.Dimension.MaximumY ||
            player.Dimension.GetPermutation(position.X, topY + 1, position.Z).Identifier is not (
                "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return false;

        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.UseOnBlock, 0);
            player.Network.Server.Events.Emit(itemUse);
            if (itemUse.IsCanceled())
                return true;
        }

        int growth = Math.Min(Random.Shared.Next(1, 4), 3 - height);
        for (int index = 0; index < growth; index++) {
            if (!player.Dimension.SetPermutation(position.X, topY + 1, position.Z, Reeds))
                break;

            topY++;
            player.Dimension.BroadcastBlockUpdate(position.X, topY, position.Z, Reeds);
            player.Dimension.UpdateNeighbors(position.X, topY, position.Z);
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
        if (y >= dimension.MaximumY || dimension.GetPermutation(x, y + 1, z).Identifier is not (
            "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return;

        int height = 1;
        while (y - height > dimension.MinimumY && dimension.GetPermutation(x, y - height, z).Identifier == block.Identifier)
            height++;
        if (height >= 3)
            return;

        int age = block.State.Values.GetValueOrDefault("age") is int value ? value : 0;
        if (age < 15) {
            if (BlockPalette.Find(block.Identifier, block.State, "age", age + 1) is { } aged &&
                dimension.SetPermutation(x, y, z, aged))
                dimension.BroadcastBlockUpdate(x, y, z, aged);
            return;
        }

        if (!dimension.SetPermutation(x, y + 1, z, Reeds) ||
            BlockPalette.Find(block.Identifier, block.State, "age", 0) is not { } reset ||
            !dimension.SetPermutation(x, y, z, reset))
            return;

        dimension.BroadcastBlockUpdate(x, y + 1, z, Reeds);
        dimension.BroadcastBlockUpdate(x, y, z, reset);
        dimension.UpdateNeighbors(x, y + 1, z);
    }

    protected override void OnSupportLost(Dimension dimension, int x, int y, int z, bool dropItem) {
        base.OnSupportLost(dimension, x, y, z, dropItem);
        dimension.UpdateNeighbors(x, y, z);
    }
}
