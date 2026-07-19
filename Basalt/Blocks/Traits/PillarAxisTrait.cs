namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;


public sealed class PillarAxisTrait : BlockTrait
{
  public static new readonly string Identifier = "pillar_axis";
  public static readonly string State = "pillar_axis";

  public PillarAxisTrait(Block block) : base(block)
  {
  }

  public override void OnPlace(BlockPlaceDetails details)
  {
    string axis = details.BlockFace switch
    {
      0 or 1 => "y",
      2 or 3 => "z",
      4 or 5 => "x",
      _ => "y"
    };

    SetAxis(axis);
  }

  public string GetAxis()
  {
    if (!Block.Permutation.State.TryGetValue(State, out BlockStateValue value) || value.Kind != 1)
    {
      return "y";
    }

    return value.AsString();
  }

  public void SetAxis(string axis)
  {
    BlockState state = [];
    foreach ((string key, BlockStateValue value) in Block.Permutation.State)
    {
      state[key] = value;
    }

    state[State] = axis;
    Block.SetPermutation(Block.Type.GetPermutation(state));
  }
}
