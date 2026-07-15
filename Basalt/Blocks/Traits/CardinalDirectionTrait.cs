namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Components;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;


public sealed class CardinalDirectionTrait : DirectionTrait
{
    public static new readonly string Identifier = "minecraft:cardinal_direction";
    public static new readonly string State = "minecraft:cardinal_direction";
    public static new readonly Type? Component = typeof(RotationComponent);

    public CardinalDirectionTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        CardinalDirection direction = RotationComponent.GetCardinalDirection(details.Player.Yaw);

        switch (direction)
        {
            case CardinalDirection.North:
                SetDirection(CardinalDirection.South);
                break;
            case CardinalDirection.South:
                SetDirection(CardinalDirection.North);
                break;
            case CardinalDirection.East:
                SetDirection(CardinalDirection.West);
                break;
            case CardinalDirection.West:
                SetDirection(CardinalDirection.East);
                break;
        }
    }

    public new CardinalDirection GetDirection()
    {
        if (!Block.Permutation.State.TryGetValue(State, out BlockStateValue value) || value.Kind != 1)
        {
            return CardinalDirection.South;
        }

        return value.AsString() switch
        {
            "north" => CardinalDirection.North,
            "south" => CardinalDirection.South,
            "east" => CardinalDirection.East,
            "west" => CardinalDirection.West,
            _ => CardinalDirection.South
        };
    }

    public void SetDirection(CardinalDirection direction)
    {
        string value = direction switch
        {
            CardinalDirection.North => "north",
            CardinalDirection.South => "south",
            CardinalDirection.East => "east",
            CardinalDirection.West => "west",
            _ => "south"
        };

        BlockState state = [];
        foreach ((string key, BlockStateValue current) in Block.Permutation.State)
        {
            state[key] = current;
        }

        state[State] = value;
        Block.SetPermutation(Block.Type.GetPermutation(state));
    }
}







