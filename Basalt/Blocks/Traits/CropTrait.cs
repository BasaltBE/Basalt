namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;


public class CropTrait : BlockTrait {
    public static new readonly string Identifier = "minecraft:crop";
    public static readonly string State = "growth";

    private const uint MinTickInterval = 600;
    private const uint MaxTickInterval = 2400;

    private const int MaxGrowth = 7;

    public CropTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        if (details.Player.Dimension is { } dimension) {
            ScheduleCropTick(dimension, details.BlockPosition);
        }
    }

    public override void OnTick(BlockTickDetails details) {
        TickCrop(details.Dimension, details.BlockPosition);
    }

    public override void OnBreak(BlockBreakDetails details) {

    }

    public bool Fertilize(Dimension dimension, BlockPos position) {
        BlockState state = Block.Permutation.State;
        if (!state.TryGetValue("growth", out BlockStateValue growthValue) || growthValue.Kind != 0) {
            return false;
        }

        int growth = (int)growthValue.AsNumber();
        int maxGrowth = GetMaxGrowth(Block.Type.Identifier);
        if (growth >= maxGrowth) {
            return false;
        }

        int nextGrowth = Math.Min(growth + Random.Shared.Next(2, 6), maxGrowth);
        BlockState nextState = [];
        foreach ((string key, BlockStateValue value) in state) {
            nextState[key] = key == "growth" ? nextGrowth : value;
        }

        BlockPermutation? nextPermutation = Block.Type.GetPermutation(nextState);
        if (nextPermutation is null) {
            return false;
        }

        dimension.SetPermutation(position.X, position.Y, position.Z, nextPermutation, 0, true);
        if (nextGrowth < maxGrowth) {
            ScheduleCropTick(dimension, position);
        }

        return true;
    }

    public override List<Item.ItemStack>? GetCustomDrops(BlockPermutation permutation) {
        if (!permutation.State.TryGetValue("growth", out BlockStateValue growthVal))
            return null;

        int currentGrowth = growthVal.Kind == 0 ? (int)growthVal.AsNumber() : 0;
        int maxGrowth = GetMaxGrowth(permutation.Type.Identifier);

        if (currentGrowth >= maxGrowth)
            return null;

        string? seedIdentifier = GetSeedForCrop(permutation.Type.Identifier);
        if (seedIdentifier is null) return null;

        Item.ItemType? seedType = Item.ItemType.Get(seedIdentifier);
        if (seedType is null) return [];

        return [new Item.ItemStack(seedType, 1)];
    }

    private static string? GetSeedForCrop(string cropIdentifier) {
        if (string.Equals(cropIdentifier, BlockIdentifier.Wheat.ToIdentifier(), StringComparison.Ordinal))
            return "minecraft:wheat_seeds";
        if (string.Equals(cropIdentifier, BlockIdentifier.Beetroot.ToIdentifier(), StringComparison.Ordinal))
            return "minecraft:beetroot_seeds";
        if (string.Equals(cropIdentifier, BlockIdentifier.MelonStem.ToIdentifier(), StringComparison.Ordinal))
            return "minecraft:melon_seeds";
        if (string.Equals(cropIdentifier, BlockIdentifier.PumpkinStem.ToIdentifier(), StringComparison.Ordinal))
            return "minecraft:pumpkin_seeds";
        if (string.Equals(cropIdentifier, BlockIdentifier.TorchflowerCrop.ToIdentifier(), StringComparison.Ordinal))
            return "minecraft:torchflower_seeds";
        return null;
    }

    public static void ScheduleCropTick(Dimension dimension, BlockPos pos, uint? customDelay = null) {
        uint delay = customDelay ?? (uint)Random.Shared.Next((int)MinTickInterval, (int)MaxTickInterval + 1);
        dimension.ScheduleBlockTick(pos, delay);
    }

    private static void TickCrop(Dimension dimension, BlockPos pos) {
        BlockPermutation perm;
        try { perm = dimension.GetPermutation(pos.X, pos.Y, pos.Z, 0); }
        catch { return; }

        if (!perm.State.TryGetValue("growth", out BlockStateValue growthVal))
            return;

        int currentGrowth = growthVal.Kind == 0 ? (int)growthVal.AsNumber() : 0;

        BlockPermutation below;
        try { below = dimension.GetPermutation(pos.X, pos.Y - 1, pos.Z, 0); }
        catch { return; }

        if (!string.Equals(below.Type.Identifier, BlockIdentifier.Farmland.ToIdentifier(), StringComparison.Ordinal)) {
            BlockPermutation air = BlockPermutation.Resolve("minecraft:air");
            dimension.RemoveBlock(pos.X, pos.Y, pos.Z);
            dimension.SetPermutation(pos.X, pos.Y, pos.Z, air);
            return;
        }

        int maxGrowth = GetMaxGrowth(perm.Type.Identifier);

        if (currentGrowth >= maxGrowth) {
            return;
        }

        int newGrowth = currentGrowth + 1;
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in perm.State) {
            if (string.Equals(key, "growth", StringComparison.Ordinal)) {
                state[key] = newGrowth;
            }
            else {
                state[key] = value;
            }
        }

        BlockPermutation? newPerm = perm.Type.GetPermutation(state);
        if (newPerm is not null) {
            dimension.SetPermutation(pos.X, pos.Y, pos.Z, newPerm, 0, true);
        }

        if (newGrowth < maxGrowth) {
            ScheduleCropTick(dimension, pos);
        }
    }

    private static int GetMaxGrowth(string blockIdentifier) {
        return MaxGrowth;
    }

}
