namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;


public class DirectionTrait : BlockTrait {
    public static new readonly string Identifier = "direction";
    public static readonly string State = "direction";
    public static new readonly Type? Component = typeof(RotationComponent);

    public DirectionTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        CardinalDirection direction = RotationComponent.GetCardinalDirection(details.Player.Yaw);

        switch (direction) {
            case CardinalDirection.North:
                SetDirection(2);
                break;
            case CardinalDirection.South:
                SetDirection(0);
                break;
            case CardinalDirection.East:
                SetDirection(3);
                break;
            case CardinalDirection.West:
                SetDirection(1);
                break;
        }
    }

    public int GetDirection() {
        if (!Block.Permutation.State.TryGetValue(State, out BlockStateValue value) || value.Kind != 0) {
            return 0;
        }

        return (int)value.AsNumber();
    }

    public void SetDirection(int direction) {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
            state[key] = value;
        }

        state[State] = direction;
        Block.SetPermutation(Block.Type.GetPermutation(state));
    }
}







