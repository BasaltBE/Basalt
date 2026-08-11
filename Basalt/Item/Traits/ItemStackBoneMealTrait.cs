using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Enums;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

namespace Basalt.Core.Item.Traits;

public sealed class ItemStackBoneMealTrait : ItemTrait {
    public new static readonly string Identifier = "bone_meal";
    public new static readonly string[] Types = ["minecraft:bone_meal"];

    public ItemStackBoneMealTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        BlockPos position = details.BlockPosition;
        Block? block = dimension.GetBlock(position.X, position.Y, position.Z);
        bool fertilized =
            block?.GetTrait<SaplingTrait>()?.Fertilize(
                dimension,
                position) == true ||
            block?.GetTrait<GrowablePlantTrait>()?.Fertilize(
                dimension,
                position) == true ||
            block?.GetTrait<CropTrait>()?.Fertilize(
                dimension,
                position) == true ||
            block?.GetTrait<GrassBlockTrait>()?.Fertilize(
                dimension,
                position) == true;
        if (!fertilized) {
            return;
        }

        dimension.Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.ParticlesCropGrowth,
            Position = new Vec3 {
                X = position.X + 0.5f,
                Y = position.Y + 0.5f,
                Z = position.Z + 0.5f
            },
            Data = 0
        });

        if (details.Player.Gamemode != GameType.Survival) {
            return;
        }

        ItemStack.DecrementStack();
        EntityInventoryTrait? inventory = details.Player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        if (ItemStack.StackSize == 0) {
            inventory.Container.ClearSlot(details.HotBarSlot);
        }
        else {
            inventory.Container.UpdateSlot(details.HotBarSlot);
        }
    }
}
