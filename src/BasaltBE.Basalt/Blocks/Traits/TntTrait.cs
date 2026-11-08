using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using GameRule = BasaltBE.Basalt.Worlds.Types.GameRule;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:tnt")]
public sealed class TntTrait : BlockTrait {
    public override bool OnInteract(Player player, BlockPos position) {
        if (player.GetTrait<PlayerInventoryTrait>() is not { } inventory ||
            inventory.Inventory.GetItem(inventory.SelectedSlot) is not { } item ||
            item.Type.Identifier is not ("minecraft:flint_and_steel" or "minecraft:fire_charge") ||
            !Prime(player.Dimension, position.X, position.Y, position.Z))
            return false;

        if (player.GameType != GameType.Creative) {
            if (item.Type.Identifier == "minecraft:fire_charge") inventory.Inventory.TakeItem(inventory.SelectedSlot);
            else item.DamageDurability();
        }

        return true;
    }

    public override void OnPlaced(Dimension dimension, int x, int y, int z) {
        if (dimension.Redstone.BlockPowered(dimension, x, y, z) == true) Prime(dimension, x, y, z);
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) =>
        dimension.Redstone.BlockPowered(dimension, x, y, z) == true && Prime(dimension, x, y, z, broadcast: false);

    internal static bool Prime(
        Dimension dimension,
        int x,
        int y,
        int z,
        int fuseTicks = 80,
        bool broadcast = true,
        bool updateNeighbors = true
    ) {
        if (dimension.World?.GameRules.Get(GameRule.TntExplodes) == false ||
            dimension.GetChunk(x >> 4, z >> 4) is null ||
            dimension.GetLoadedPermutation(x, y, z)?.Identifier != "minecraft:tnt" ||
            !dimension.SetPermutation(x, y, z, BlockStorage.Air))
            return false;

        if (broadcast) dimension.BroadcastBlockUpdate(x, y, z, BlockStorage.Air);
        if (updateNeighbors) dimension.UpdateNeighbors(x, y, z);
        Entity tnt = new("minecraft:tnt") {
            Position = { X = x + 0.5f, Y = y, Z = z + 0.5f }
        };
        tnt.GetTrait<PrimedTntTrait>()!.FuseTicks = Math.Max(1, fuseTicks);
        if (tnt.GetTrait<EntityMovementTrait>() is { } movement) {
            movement.Gravity = 0.04f;
            movement.Drag = 0.02f;
            movement.Motion.X = (Random.Shared.NextSingle() - 0.5f) * 0.02f;
            movement.Motion.Y = 0.2f;
            movement.Motion.Z = (Random.Shared.NextSingle() - 0.5f) * 0.02f;
        }
        tnt.Spawn(dimension);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "fuse",
            Position = new Vec3 { X = tnt.Position.X, Y = tnt.Position.Y + 0.5f, Z = tnt.Position.Z },
            ActorIdentifier = "minecraft:tnt",
            ActorUniqueId = tnt.UniqueId
        }, x >> 4, z >> 4);
        return true;
    }
}
