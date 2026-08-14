using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

namespace Basalt.Core.Item.Traits;

public sealed class ItemStackBoneMealTrait : ItemTrait {
    private const ulong CooldownTicks = 2;

    public new static readonly string Identifier = "bone_meal";
    public new static readonly string[] Types = ["minecraft:bone_meal"];
    private ulong? _lastUseTick;

    public ItemStackBoneMealTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        World? world = dimension.World;
        if (world is null) {
            return;
        }

        ulong currentTick = world.TickValue;
        if (_lastUseTick is { } lastUseTick && currentTick - lastUseTick < CooldownTicks) {
            return;
        }

        BlockPos position = details.BlockPosition;
        Block? block = dimension.GetBlock(position.X, position.Y, position.Z);
        FlowerTrait? flower = block?.GetTrait<FlowerTrait>();
        GrassBlockTrait? grass = block?.GetTrait<GrassBlockTrait>();
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
            flower?.Fertilize(
                dimension,
                position) == true ||
            grass?.Fertilize(
                dimension,
                position) == true;
        if (!fertilized) {
            return;
        }

        _lastUseTick = currentTick;

        IReadOnlyList<BlockPos> particlePositions = flower?.AffectedPositions.Count > 0
            ? flower.AffectedPositions
            : grass?.AffectedPositions.Count > 0
                ? grass.AffectedPositions
                : [position];
        foreach (BlockPos particlePosition in particlePositions) {
            dimension.Broadcast(new SpawnParticleEffectPacket {
                DimensionId = (byte)dimension.Type,
                ActorId = new ActorUniqueID { Value = -1 },
                Position = new Vec3 {
                    X = particlePosition.X + 0.5f,
                    Y = particlePosition.Y + 0.5f,
                    Z = particlePosition.Z + 0.5f
                },
                EffectName = "minecraft:crop_growth_emitter"
            });
        }

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
