namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Blocks;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Types;
using Player = Player.Player;

public static class FillCommand {
  const int MaxVolume = 65536;
  const int BlocksPerTick = 4096;

  public static readonly CommandDefinition Definition = new() {
    Name = "fill",
    Description = "Fills a region with a specified block.",
    Permissions = ["basalt.op"],
    Overloads =
    [
      new OverloadDefinition
      {
        Parameters =
        [
          new ParameterDefinition { Name = "from", Type = typeof(PositionEnum) },
          new ParameterDefinition { Name = "to", Type = typeof(PositionEnum) },
          new ParameterDefinition { Name = "block", Type = typeof(BlockEnum) }
        ]
      }
    ],
    Handler = new CommandHandler(Execute)
  };

  static CommandResult Execute(CommandContext ctx) {
    Player? player = ctx.RequirePlayer(out CommandResult? err);
    if (player is null) return err!;

    Dimension? dimension = player.Dimension;
    if (dimension is null)
      return CommandResult.Error("No dimension available.");

    PositionEnum? from = ctx.Get<PositionEnum>("from");
    PositionEnum? to = ctx.Get<PositionEnum>("to");
    BlockEnum? block = ctx.Get<BlockEnum>("block");

    if (from is null || to is null || block is null)
      return CommandResult.Error("Usage: /fill <from: x y z> <to: x y z> <block>");

    int x1 = (int)MathF.Floor(from.Value.X);
    int y1 = (int)MathF.Floor(from.Value.Y);
    int z1 = (int)MathF.Floor(from.Value.Z);
    int x2 = (int)MathF.Floor(to.Value.X);
    int y2 = (int)MathF.Floor(to.Value.Y);
    int z2 = (int)MathF.Floor(to.Value.Z);

    int minX = Math.Min(x1, x2);
    int minY = Math.Min(y1, y2);
    int minZ = Math.Min(z1, z2);
    int maxX = Math.Max(x1, x2);
    int maxY = Math.Max(y1, y2);
    int maxZ = Math.Max(z1, z2);

    int sizeX = maxX - minX + 1;
    int sizeY = maxY - minY + 1;
    int sizeZ = maxZ - minZ + 1;
    long volume = (long)sizeX * sizeY * sizeZ;

    if (volume > MaxVolume)
      return CommandResult.Error($"Fill volume too large ({volume} blocks). Maximum is {MaxVolume}.");

    BlockPermutation permutation = block.Type.GetPermutation();

    if (volume <= BlocksPerTick) {
      int filled = dimension.Fill(minX, minY, minZ, maxX, maxY, maxZ, permutation);
      return CommandResult.OkMessage($"§7Filled §a{filled}§7 blocks.");
    }

    FillTask task = new(dimension, minX, minY, minZ, maxX, maxY, maxZ, permutation);
    dimension.World?.Scheduler?.Schedule(task);
    return CommandResult.OkMessage($"§7Filling §a{volume}§7 blocks in background...");
  }

  sealed class FillTask : ServerTask {
    private readonly Dimension _dimension;
    private readonly int _minX, _minY, _minZ;
    private readonly int _maxX, _maxY, _maxZ;
    private readonly BlockPermutation _permutation;
    private int _currentX, _currentY, _currentZ;

    public FillTask(
      Dimension dimension,
      int minX, int minY, int minZ,
      int maxX, int maxY, int maxZ,
      BlockPermutation permutation) {
      RunOnMainThread = true;
      _dimension = dimension;
      _minX = minX; _minY = minY; _minZ = minZ;
      _maxX = maxX; _maxY = maxY; _maxZ = maxZ;
      _permutation = permutation;
      _currentX = minX; _currentY = minY; _currentZ = minZ;
    }

    public override void Execute() {
      int batchMinX = _currentX, batchMinY = _currentY, batchMinZ = _currentZ;
      int batchMaxX = _currentX, batchMaxY = _currentY, batchMaxZ = _currentZ;
      int count = 0;

      // Compute the end position for this batch.
      while (count < BlocksPerTick) {
        batchMaxX = _currentX;
        batchMaxY = _currentY;
        batchMaxZ = _currentZ;
        count++;

        if (!Advance()) {
          _dimension.Fill(batchMinX, batchMinY, batchMinZ, _maxX, _maxY, _maxZ, _permutation);
          return;
        }
      }

      _dimension.Fill(batchMinX, _minY, _minZ, batchMaxX, _maxY, _maxZ, _permutation);

      // Re-schedule for the next batch.
      _currentX = batchMaxX;
      _currentY = batchMaxY;
      _currentZ = batchMaxZ;
      Advance();
      _dimension.World?.Scheduler?.Schedule(this);
    }

    public override void Complete() {
      IsExecuted = false;
      IsCompleted = false;
    }

    private bool Advance() {
      _currentY++;
      if (_currentY > _maxY) {
        _currentY = _minY;
        _currentZ++;
        if (_currentZ > _maxZ) {
          _currentZ = _minZ;
          _currentX++;
          if (_currentX > _maxX) {
            return false;
          }
        }
      }
      return true;
    }
  }
}
