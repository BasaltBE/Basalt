namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Types;

internal sealed class HopperTickTask : DelayedTask
{
  private readonly Dimension _dimension;
  private readonly BlockPos _position;

  public HopperTickTask(Dimension dimension, BlockPos position)
  {
    _dimension = dimension;
    _position = position;
  }

  public override void Execute()
  {
    Block? block = _dimension.GetBlock(_position.X, _position.Y, _position.Z);
    HopperTrait? trait = block?.GetTrait<HopperTrait>();

    if (trait is null) return;

    bool shouldContinue = trait.Tick();
    if (!shouldContinue)
    {
      trait.MarkTickingStopped();
      return;
    }

    Server? server = _dimension.World?.Server;
    if (server is null)
    {
      trait.MarkTickingStopped();
      return;
    }

    server.Scheduler.Schedule(
      new HopperTickTask(_dimension, _position) { DelayTicks = 1, RunOnMainThread = true },
      _dimension.World!.TickValue);
  }
}
