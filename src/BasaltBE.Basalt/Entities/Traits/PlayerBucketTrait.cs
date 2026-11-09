using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerBucketTrait(Entity entity) : EntityTrait(entity) {
    public bool UseOnBlock(
        PlayerInventoryTrait inventory,
        ItemStack? item,
        BlockPermutation clicked,
        BlockPos position,
        byte face
    ) {
        if (item is null || face > 5) return false;

        bool collecting = item.Type.Identifier == "minecraft:bucket";
        bool water = item.Type.Identifier == "minecraft:water_bucket";
        bool lava = item.Type.Identifier == "minecraft:lava_bucket";
        if (!collecting && !water && !lava) return false;

        Player player = (Player)Entity;
        if (collecting) {
            if (clicked.Identifier is not ("minecraft:water" or "minecraft:lava") ||
                clicked.State.Values.GetValueOrDefault("liquid_depth") is not 0)
                return false;

            string filledBucket = clicked.Identifier == "minecraft:water"
                ? "minecraft:water_bucket"
                : "minecraft:lava_bucket";
            BlockPermutation air = BlockPalette.FindDefaultPermutation("minecraft:air")!;
            bool creative = player.Abilities.Get(PlayerAbility.InstantBuild);
            if (!creative && !player.Dimension.SetPermutation(position.X, position.Y, position.Z, air)) return false;

            inventory.Inventory.SetItem(inventory.SelectedSlot, new ItemStack(filledBucket));
            if (!creative) UpdateFluid(player.Dimension, position.X, position.Y, position.Z, air);
            PlaySound(player, position, clicked.Identifier == "minecraft:water" ? "bucket_fill_water" : "bucket_fill_lava");
            return true;
        }

        if (!player.Abilities.Get(PlayerAbility.Build)) return false;

        int x = position.X;
        int y = position.Y;
        int z = position.Z;
        if (!BlockPalette.Replaceable(clicked)) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            x += offsetX;
            y += offsetY;
            z += offsetZ;
        }
        if (y < player.Dimension.MinimumY || y > player.Dimension.MaximumY ||
            player.Dimension.GetLoadedPermutation(x, y, z) is not { } target ||
            !BlockPalette.Replaceable(target))
            return false;

        if (target.Identifier is "minecraft:water" or "minecraft:flowing_water" or
            "minecraft:lava" or "minecraft:flowing_lava") {
            bool targetWater = target.Identifier is "minecraft:water" or "minecraft:flowing_water";
            if (water == targetWater) {
                if (target.Identifier is "minecraft:water" or "minecraft:lava") return false;

                BlockPermutation source = BlockPalette.FindDefaultPermutation(water ? "minecraft:water" : "minecraft:lava")!;
                if (!player.Dimension.SetPermutation(x, y, z, source)) return false;
                if (!player.Abilities.Get(PlayerAbility.InstantBuild))
                    inventory.Inventory.SetItem(inventory.SelectedSlot, new ItemStack("minecraft:bucket"));
                UpdateFluid(player.Dimension, x, y, z, source);
                PlaySound(player, new BlockPos { X = x, Y = y, Z = z }, water ? "bucket_empty_water" : "bucket_empty_lava");
                return true;
            }

            string solidIdentifier = (target.Identifier is "minecraft:water" or "minecraft:lava")
                ? "minecraft:obsidian"
                : "minecraft:cobblestone";
            BlockPermutation solid = BlockPalette.FindDefaultPermutation(solidIdentifier)!;
            if (!player.Dimension.SetPermutation(x, y, z, solid)) return false;
            if (!player.Abilities.Get(PlayerAbility.InstantBuild))
                inventory.Inventory.SetItem(inventory.SelectedSlot, new ItemStack("minecraft:bucket"));
            UpdateFluid(player.Dimension, x, y, z, solid);
            PlaySound(player, new BlockPos { X = x, Y = y, Z = z }, water ? "bucket_empty_water" : "bucket_empty_lava");
            return true;
        }

        string fluidIdentifier = water ? "minecraft:water" : "minecraft:lava";
        BlockPermutation fluid = BlockPalette.FindDefaultPermutation(fluidIdentifier)!;
        if (!player.Dimension.SetPermutation(x, y, z, fluid)) return false;

        if (!player.Abilities.Get(PlayerAbility.InstantBuild))
            inventory.Inventory.SetItem(inventory.SelectedSlot, new ItemStack("minecraft:bucket"));
        UpdateFluid(player.Dimension, x, y, z, fluid);
        PlaySound(player, new BlockPos { X = x, Y = y, Z = z }, water ? "bucket_empty_water" : "bucket_empty_lava");
        return true;
    }

    static void UpdateFluid(Worlds.Dimensions.Dimension dimension, int x, int y, int z, BlockPermutation fluid) {
        dimension.BroadcastBlockUpdate(x, y, z, fluid);
        dimension.UpdateNeighbors(x, y, z);
        BlockPalette.OnPlaced(fluid.Identifier, dimension, x, y, z);
    }

    static void PlaySound(Player player, BlockPos position, string sound) {
        player.Dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = sound,
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        }, position.X >> 4, position.Z >> 4);
    }
}
