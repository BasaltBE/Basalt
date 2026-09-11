namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Entities;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class PressurePlateTrait : BlockTrait {
    public static new readonly string[] Types = [
        BlockIdentifier.AcaciaPressurePlate.ToIdentifier(), BlockIdentifier.BambooPressurePlate.ToIdentifier(),
        BlockIdentifier.BirchPressurePlate.ToIdentifier(), BlockIdentifier.CherryPressurePlate.ToIdentifier(),
        BlockIdentifier.CrimsonPressurePlate.ToIdentifier(), BlockIdentifier.DarkOakPressurePlate.ToIdentifier(),
        BlockIdentifier.HeavyWeightedPressurePlate.ToIdentifier(), BlockIdentifier.JunglePressurePlate.ToIdentifier(),
        BlockIdentifier.LightWeightedPressurePlate.ToIdentifier(), BlockIdentifier.MangrovePressurePlate.ToIdentifier(),
        BlockIdentifier.PaleOakPressurePlate.ToIdentifier(), BlockIdentifier.PolishedBlackstonePressurePlate.ToIdentifier(),
        BlockIdentifier.PoplarPressurePlate.ToIdentifier(), BlockIdentifier.SprucePressurePlate.ToIdentifier(),
        BlockIdentifier.StonePressurePlate.ToIdentifier(), BlockIdentifier.WarpedPressurePlate.ToIdentifier(),
        BlockIdentifier.WoodenPressurePlate.ToIdentifier()
    ];

    private const string StateSignal = "redstone_signal";

    public PressurePlateTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension dimension = details.Player.Dimension!;
        if (!HasSupport(dimension, details.BlockPosition)) {
            dimension.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        dimension.ScheduleBlockTick(details.BlockPosition, 1);
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        details.Dimension.ScheduleBlockTick(details.BlockPosition, 1);
    }

    public override void OnTick(BlockTickDetails details) {
        if (!HasSupport(details.Dimension, details.BlockPosition)) {
            details.Dimension.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        int signal = GetSignal(details.Dimension, details.BlockPosition);
        int current = Block.Permutation.State.TryGetValue(StateSignal, out BlockStateValue value) && value.Kind == 0
            ? Math.Clamp((int)value.AsNumber(), 0, 15)
            : 0;
        if (signal != current) {
            BlockState state = [];
            foreach ((string key, BlockStateValue stateValue) in Block.Permutation.State) state[key] = stateValue;
            state[StateSignal] = signal;
            details.Dimension.SetPermutation(details.BlockPosition.X, details.BlockPosition.Y, details.BlockPosition.Z, Block.Type.GetPermutation(state));
            details.Dimension.UpdateRedstone(details.BlockPosition);
        }

        details.Dimension.ScheduleBlockTick(details.BlockPosition, signal > 0 ? (uint)GetPressedTime() : 1);
    }

    private int GetSignal(Dimension dimension, BlockPos position) {
        bool mobsOnly = Block.Type.Identifier == BlockIdentifier.StonePressurePlate.ToIdentifier() ||
            Block.Type.Identifier == BlockIdentifier.PolishedBlackstonePressurePlate.ToIdentifier();
        int maximum = Block.Type.Identifier == BlockIdentifier.LightWeightedPressurePlate.ToIdentifier() ? 15 :
            Block.Type.Identifier == BlockIdentifier.HeavyWeightedPressurePlate.ToIdentifier() ? 150 : 1;
        int count = 0;
        HashSet<Entity> entities = [];
        for (int x = (position.X >> 4) - 1; x <= (position.X >> 4) + 1; x++) {
            for (int z = (position.Z >> 4) - 1; z <= (position.Z >> 4) + 1; z++) entities.UnionWith(dimension.GetEntities(x, z));
        }

        foreach (Entity entity in entities) {
            if (!entity.IsAlive || entity.PendingDespawn || (mobsOnly && entity is ItemEntity) ||
                entity.Position.X < position.X + 0.0625 || entity.Position.X > position.X + 0.9375 ||
                entity.Position.Z < position.Z + 0.0625 || entity.Position.Z > position.Z + 0.9375 ||
                entity.Position.Y < position.Y || entity.Position.Y > position.Y + 1.5) continue;
            count++;
        }

        return maximum == 1 ? (count > 0 ? 15 : 0) : (int)Math.Ceiling(Math.Min(count, maximum) * 15d / maximum);
    }

    private int GetPressedTime() => Block.Type.Identifier == BlockIdentifier.LightWeightedPressurePlate.ToIdentifier() ||
        Block.Type.Identifier == BlockIdentifier.HeavyWeightedPressurePlate.ToIdentifier() ? 10 : 20;

    private static bool HasSupport(Dimension dimension, BlockPos position) => dimension.GetLoadedPermutationOrAir(position.X, position.Y - 1, position.Z).Type.Solid;
}
