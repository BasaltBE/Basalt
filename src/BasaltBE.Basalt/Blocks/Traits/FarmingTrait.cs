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

[BlockTraitAppliesTo(
    "minecraft:farmland",
    "minecraft:wheat",
    "minecraft:carrots",
    "minecraft:potatoes",
    "minecraft:beetroot",
    "minecraft:melon_stem",
    "minecraft:pumpkin_stem"
)]
public sealed class FarmingTrait : BlockTrait {
    static readonly BlockPermutation Dirt = BlockPalette.FindDefaultPermutation("minecraft:dirt")!;
    static readonly BlockPermutation Air = BlockPalette.FindDefaultPermutation("minecraft:air")!;
    internal override bool HasRandomTicks => true;

    public override bool OnUseItemOnBlock(
        Player player,
        ItemStack? item,
        BlockPermutation block,
        BlockPos position,
        byte face,
        Vec3 clickPosition
    ) {
        if (item?.Type.Identifier != "minecraft:bone_meal" || !Crop(block.Identifier) ||
            Stem(block.Identifier) && block.State.Values.GetValueOrDefault("facing_direction") is int attached && attached >= 2)
            return false;

        int growth = block.State.Values.GetValueOrDefault("growth") is int age ? age : 0;
        int maximum = block.Identifier == "minecraft:beetroot" ? 3 : 7;
        if (growth >= maximum ||
            BlockPalette.Find(block.Identifier, block.State, "growth", Math.Min(maximum, growth + Random.Shared.Next(2, 6))) is not { } grown)
            return false;

        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.UseOnBlock, 0);
            player.Network.Server.Events.Emit(itemUse);
            if (itemUse.IsCanceled()) return true;
        }

        player.Dimension.SetPermutation(position.X, position.Y, position.Z, grown);
        player.Dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, grown);
        player.Dimension.Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.ParticlesCropGrowth,
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            Data = 15
        }, new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f });
        player.Dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "item.bone_meal.use",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        }, new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f });

        if (!player.Abilities.Get(PlayerAbility.InstantBuild)) item.DecrementStack();
        return true;
    }

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) =>
        !Crop(permutation.Identifier) || y > player.Dimension.MinimumY &&
            player.Dimension.GetPermutation(x, y - 1, z).Identifier == "minecraft:farmland";

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation block = dimension.GetPermutation(x, y, z);
        if (Crop(block.Identifier)) {
            if (y > dimension.MinimumY && dimension.GetPermutation(x, y - 1, z).Identifier == "minecraft:farmland") return false;

            return dimension.SetPermutation(x, y, z, Air);
        }

        if (block.Identifier != "minecraft:farmland") return false;

        BlockType? below = y > dimension.MinimumY
            ? BlockPalette.FindType(dimension.GetPermutation(x, y - 1, z).Identifier)
            : null;
        BlockPermutation above = y < dimension.MaximumY ? dimension.GetPermutation(x, y + 1, z) : Air;
        bool aboveCanMaintain = !BlockPalette.FindType(above.Identifier)?.Solid ?? true;
        if (below?.Solid == true && (aboveCanMaintain || Crop(above.Identifier))) return false;

        return dimension.SetPermutation(x, y, z, Dirt);
    }

    public override void OnRandomTick(Dimension dimension, BlockPermutation block, int x, int y, int z) {
        if (block.Identifier == "minecraft:farmland") {
            int moisture = block.State.Values.GetValueOrDefault("moisturized_amount") is int amount ? amount : 0;
            bool waterNearby = false;
            for (int waterY = 0; waterY <= 1 && !waterNearby; waterY++) {
                for (int waterX = -4; waterX <= 4 && !waterNearby; waterX++) {
                    for (int waterZ = -4; waterZ <= 4; waterZ++) {
                        string neighbor = dimension.GetPermutation(x + waterX, y + waterY, z + waterZ).Identifier;
                        if (neighbor is "minecraft:water" or "minecraft:flowing_water") {
                            waterNearby = true;
                            break;
                        }
                    }
                }
            }

            if (waterNearby) {
                if (moisture < 7 && BlockPalette.Find(block.Identifier, block.State, "moisturized_amount", 7) is { } hydrated &&
                    dimension.SetPermutation(x, y, z, hydrated)) {
                    dimension.BroadcastBlockUpdate(x, y, z, hydrated);
                }
                return;
            }

            if (moisture > 0) {
                if (BlockPalette.Find(block.Identifier, block.State, "moisturized_amount", moisture - 1) is { } dry &&
                    dimension.SetPermutation(x, y, z, dry)) {
                    dimension.BroadcastBlockUpdate(x, y, z, dry);
                }
            } else {
                BlockPermutation above = y < dimension.MaximumY ? dimension.GetPermutation(x, y + 1, z) : Air;
                if (!Crop(above.Identifier) && dimension.SetPermutation(x, y, z, Dirt)) {
                    dimension.BroadcastBlockUpdate(x, y, z, Dirt);
                    dimension.UpdateNeighbors(x, y, z, block);
                }
            }
            return;
        }

        if (!Crop(block.Identifier) || y <= dimension.MinimumY ||
            dimension.GetPermutation(x, y - 1, z).Identifier != "minecraft:farmland")
            return;

        if (Stem(block.Identifier) && block.State.Values.GetValueOrDefault("facing_direction") is int attached && attached >= 2)
            return;

        int growth = block.State.Values.GetValueOrDefault("growth") is int age ? age : 0;
        int maximum = block.Identifier == "minecraft:beetroot" ? 3 : 7;
        bool stem = Stem(block.Identifier);
        if (!stem && growth >= maximum) return;

        float speed = GrowthSpeed(dimension, block, x, y, z);
        if (Random.Shared.Next((int)(25 / speed) + 1) != 0) return;

        if (growth < maximum) {
            if (BlockPalette.Find(block.Identifier, block.State, "growth", growth + 1) is { } grown &&
                dimension.SetPermutation(x, y, z, grown)) {
                dimension.BroadcastBlockUpdate(x, y, z, grown);
            }
            return;
        }

        if (!stem) return;


        int face = Random.Shared.Next(2, 6);
        (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
        int fruitX = x + offsetX;
        int fruitY = y + offsetY;
        int fruitZ = z + offsetZ;
        if (dimension.GetPermutation(fruitX, fruitY, fruitZ).Identifier != "minecraft:air" ||
            fruitY <= dimension.MinimumY || !DirtSoil(dimension.GetPermutation(fruitX, fruitY - 1, fruitZ).Identifier))
            return;

        string fruitIdentifier = block.Identifier == "minecraft:melon_stem" ? "minecraft:melon_block" : "minecraft:pumpkin";
        if (BlockPalette.FindDefaultPermutation(fruitIdentifier) is not { } fruit ||
            BlockPalette.Find(block.Identifier, block.State, "facing_direction", face) is not { } attachedStem)
            return;

        if (!dimension.SetPermutation(fruitX, fruitY, fruitZ, fruit) ||
            !dimension.SetPermutation(x, y, z, attachedStem))
            return;

        dimension.BroadcastBlockUpdate(fruitX, fruitY, fruitZ, fruit);
        dimension.BroadcastBlockUpdate(x, y, z, attachedStem);
        dimension.UpdateNeighbors(fruitX, fruitY, fruitZ);
        dimension.UpdateNeighbors(x, y, z, block);
    }

    float GrowthSpeed(Dimension dimension, BlockPermutation crop, int x, int y, int z) {
        float speed = 1;
        for (int offsetX = -1; offsetX <= 1; offsetX++) {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++) {
                if (y <= dimension.MinimumY || dimension.GetPermutation(x + offsetX, y - 1, z + offsetZ) is not { Identifier: "minecraft:farmland" } soil)
                    continue;

                float blockSpeed = soil.State.Values.GetValueOrDefault("moisturized_amount") is int moisture && moisture > 0 ? 3 : 1;
                speed += offsetX == 0 && offsetZ == 0 ? blockSpeed : blockSpeed / 4;
            }
        }

        bool horizontal = SameCrop(dimension, crop.Identifier, x - 1, y, z) || SameCrop(dimension, crop.Identifier, x + 1, y, z);
        bool vertical = SameCrop(dimension, crop.Identifier, x, y, z - 1) || SameCrop(dimension, crop.Identifier, x, y, z + 1);
        if (horizontal && vertical) return speed / 2;
        if (SameCrop(dimension, crop.Identifier, x - 1, y, z - 1) ||
            SameCrop(dimension, crop.Identifier, x + 1, y, z - 1) ||
            SameCrop(dimension, crop.Identifier, x - 1, y, z + 1) ||
            SameCrop(dimension, crop.Identifier, x + 1, y, z + 1))
            speed /= 2;

        return speed;
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z, BlockPermutation neighbor) {
        BlockPermutation block = dimension.GetPermutation(x, y, z);
        if (!Stem(block.Identifier) || block.State.Values.GetValueOrDefault("facing_direction") is not int face || face < 2)
            return OnNeighborUpdate(dimension, x, y, z);

        if ((uint)face >= BlockDirections.OffsetByFace.Length) return false;

        (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
        BlockPermutation fruit = dimension.GetPermutation(x + offsetX, y + offsetY, z + offsetZ);
        string fruitIdentifier = block.Identifier == "minecraft:melon_stem" ? "minecraft:melon_block" : "minecraft:pumpkin";
        if (fruit.Identifier == fruitIdentifier) return false;

        if (BlockPalette.Find(block.Identifier, block.State, "facing_direction", 0) is not { } stem ||
            !dimension.SetPermutation(x, y, z, stem))
            return false;

        return true;
    }

    static bool SameCrop(Dimension dimension, string identifier, int x, int y, int z) =>
        dimension.GetPermutation(x, y, z).Identifier == identifier;

    static bool Crop(string identifier) => identifier is
        "minecraft:wheat" or "minecraft:carrots" or "minecraft:potatoes" or "minecraft:beetroot" or
        "minecraft:melon_stem" or "minecraft:pumpkin_stem";

    static bool Stem(string identifier) => identifier is "minecraft:melon_stem" or "minecraft:pumpkin_stem";

    static bool DirtSoil(string identifier) =>
        BlockPalette.FindType(identifier)?.Tags.Contains("dirt", StringComparer.Ordinal) == true;
}
