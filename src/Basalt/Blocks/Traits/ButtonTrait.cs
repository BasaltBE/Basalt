namespace Basalt.Core.Blocks.Traits;

using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Enums;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Worlds.Dimensions;

public sealed class ButtonTrait : BlockTrait {
    public static new readonly string[] Types = [
        "minecraft:acacia_button",
        "minecraft:bamboo_button",
        "minecraft:birch_button",
        "minecraft:cherry_button",
        "minecraft:crimson_button",
        "minecraft:dark_oak_button",
        "minecraft:jungle_button",
        "minecraft:mangrove_button",
        "minecraft:pale_oak_button",
        "minecraft:poplar_button",
        "minecraft:polished_blackstone_button",
        "minecraft:spruce_button",
        "minecraft:stone_button",
        "minecraft:warped_button",
        "minecraft:wooden_button"
    ];

    private const string StateFacing = "facing_direction";
    private const string StatePressed = "button_pressed_bit";

    public ButtonTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) return;

        if (!HasSupport(dimension, details.BlockPosition, details.BlockFace)) {
            dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StateFacing] = details.BlockFace;
        state[StatePressed] = false;
        dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            Block.Type.GetPermutation(state));
        dimension.UpdateRedstone(details.BlockPosition);
    }

    public override void OnInteract(BlockInteractDetails details) {
        Dimension dimension = details.Player.Dimension!;
        if (IsPressed()) return;

        SetPressed(dimension, details.BlockPosition, true);
        dimension.PlaySound(
            SoundIdentifier.ButtonClickOn,
            new Vec3 {
                X = details.BlockPosition.X + 0.5f,
                Y = details.BlockPosition.Y + 0.5f,
                Z = details.BlockPosition.Z + 0.5f
            });
        dimension.ScheduleBlockTick(details.BlockPosition, IsWooden() ? 30u : 20u);
    }

    public override void OnTick(BlockTickDetails details) {
        if (!HasSupport(details.Dimension, details.BlockPosition, GetFacing())) {
            details.Dimension.SetPermutation(
                details.BlockPosition.X,
                details.BlockPosition.Y,
                details.BlockPosition.Z,
                BlockPermutation.Resolve(BlockIdentifier.Air));
            return;
        }

        if (IsPressed()) {
            SetPressed(details.Dimension, details.BlockPosition, false);
            details.Dimension.PlaySound(
                SoundIdentifier.ButtonClickOff,
                new Vec3 {
                    X = details.BlockPosition.X + 0.5f,
                    Y = details.BlockPosition.Y + 0.5f,
                    Z = details.BlockPosition.Z + 0.5f
                });
        }
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        if (HasSupport(details.Dimension, details.BlockPosition, GetFacing())) return;

        details.Dimension.SetPermutation(
            details.BlockPosition.X,
            details.BlockPosition.Y,
            details.BlockPosition.Z,
            BlockPermutation.Resolve(BlockIdentifier.Air));
    }

    private bool IsPressed() {
        return Block.Permutation.State.TryGetValue(StatePressed, out BlockStateValue value) &&
            value.Kind == 2 && value.AsBool();
    }

    private int GetFacing() {
        return Block.Permutation.State.TryGetValue(StateFacing, out BlockStateValue value) && value.Kind == 0
            ? Math.Clamp((int)value.AsNumber(), 0, 5)
            : 1;
    }

    private void SetPressed(Dimension dimension, BlockPos position, bool pressed) {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[StatePressed] = pressed;
        dimension.SetPermutation(position.X, position.Y, position.Z, Block.Type.GetPermutation(state));
        dimension.UpdateRedstone(position);
    }

    private bool IsWooden() {
        return !string.Equals(Block.Type.Identifier, BlockIdentifier.StoneButton.ToIdentifier(), StringComparison.Ordinal) &&
            !string.Equals(Block.Type.Identifier, BlockIdentifier.PolishedBlackstoneButton.ToIdentifier(), StringComparison.Ordinal);
    }

    private static bool HasSupport(Dimension dimension, BlockPos position, int facing) {
        BlockPos support = facing switch {
            0 => new BlockPos { X = position.X, Y = position.Y + 1, Z = position.Z },
            1 => new BlockPos { X = position.X, Y = position.Y - 1, Z = position.Z },
            2 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z + 1 },
            3 => new BlockPos { X = position.X, Y = position.Y, Z = position.Z - 1 },
            4 => new BlockPos { X = position.X + 1, Y = position.Y, Z = position.Z },
            _ => new BlockPos { X = position.X - 1, Y = position.Y, Z = position.Z }
        };

        return dimension.GetLoadedPermutationOrAir(support.X, support.Y, support.Z).Type.Solid;
    }
}
