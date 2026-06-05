namespace Basalt.Core.Block.Traits;

using Basalt.Core.Block.Components;
using Basalt.Core.Block.Traits.Types;
using Basalt.Core.Block.Types;


public sealed class FacingDirectionTrait : DirectionTrait
{
    public static new readonly string Identifier = "facing_direction";
    public static new readonly string State = "facing_direction";
    public static new readonly Type? Component = typeof(BlockTypeRotationComponent);

    public FacingDirectionTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        int pitch = (int)MathF.Ceiling(details.Player.Pitch);
        if (pitch == 90)
        {
            SetDirection(FacingDirection.Up);
            return;
        }

        if (pitch == -90)
        {
            SetDirection(FacingDirection.Down);
            return;
        }

        CardinalDirection direction = BlockTypeRotationComponent.GetCardinalDirection(details.Player.Yaw);
        switch (direction)
        {
            case CardinalDirection.North:
                SetDirection(FacingDirection.South);
                break;
            case CardinalDirection.South:
                SetDirection(FacingDirection.North);
                break;
            case CardinalDirection.East:
                SetDirection(FacingDirection.West);
                break;
            case CardinalDirection.West:
                SetDirection(FacingDirection.East);
                break;
        }
    }

    public new FacingDirection GetDirection()
    {
        if (!Block.Permutation.State.TryGetValue(State, out BlockStateValue value) || value.Kind != 0)
        {
            return FacingDirection.South;
        }

        return (FacingDirection)(int)value.AsNumber();
    }

    public void SetDirection(FacingDirection direction)
    {
        BlockState state = [];
        foreach ((string key, BlockStateValue value) in Block.Permutation.State)
        {
            state[key] = value;
        }

        state[State] = (int)direction;
        Block.SetPermutation(Block.Type.GetPermutation(state));
    }
}







