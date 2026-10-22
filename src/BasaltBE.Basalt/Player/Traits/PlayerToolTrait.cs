using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using PlayerEntity = BasaltBE.Basalt.Entities.Player;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerToolTrait(Entity entity) : EntityTrait(entity) {
    public override void OnUseItemOnBlock(ItemStack? item, BlockPermutation block, BlockPos position, byte face, Vec3 clickPosition) {
        if (item is null || Entity is not PlayerEntity player)
            return;

        BlockPermutation? updated;
        switch (item.Type.ToolType) {
            case ToolType.Hoe when face == 1:
                updated = Block.Till(block);
                break;
            case ToolType.Shovel when face == 1 && block.Identifier is
                "minecraft:grass_block" or "minecraft:dirt" or "minecraft:coarse_dirt" or
                "minecraft:podzol" or "minecraft:mycelium" or "minecraft:dirt_with_roots":
                updated = BlockPalette.FindDefaultPermutation("minecraft:grass_path");
                break;
            case ToolType.Axe:
                updated = BlockPalette.FindStripped(block);
                break;
            default:
                return;
        }

        if (item.Type.ToolType is ToolType.Hoe or ToolType.Shovel &&
            (position.Y >= 319 || player.Dimension.GetPermutation(position.X, position.Y + 1, position.Z).Identifier is not (
                "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air")))
            return;
        if (updated is null || updated == block)
            return;

        int durabilityDamage = 1;
        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.UseOnBlock, durabilityDamage);
            player.Network.Server.Events.Emit(itemUse);
            durabilityDamage = itemUse.DurabilityDamage;
            if (itemUse.IsCanceled()) {
                if (!player.Abilities.Get(PlayerAbility.InstantBuild))
                    item.DamageDurability(durabilityDamage);
                return;
            }
        }

        player.Dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = unchecked((uint)updated.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, position.X >> 4, position.Z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = "item.use.on",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            Data = updated.NetworkId,
            ActorUniqueId = -1
        });
        player.Dimension.UpdateNeighbors(position.X, position.Y, position.Z);
        if (!player.Abilities.Get(PlayerAbility.InstantBuild))
            item.DamageDurability(durabilityDamage);
    }

}
