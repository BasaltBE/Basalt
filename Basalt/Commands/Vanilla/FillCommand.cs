namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Blocks;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Enums;
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
      int filled = FillRegion(dimension, minX, minY, minZ, maxX, maxY, maxZ, permutation);
      return CommandResult.OkMessage($"§7Filled §a{filled}§7 blocks.");
    }

    FillTask task = new(dimension, minX, minY, minZ, maxX, maxY, maxZ, permutation);
    dimension.World?.Scheduler?.Schedule(task);
    return CommandResult.OkMessage($"§7Filling §a{volume}§7 blocks in background...");
  }

  internal static int FillRegion(
    Dimension dimension, int minX, int minY, int minZ, int maxX, int maxY, int maxZ,
    BlockPermutation permutation) {
    int filled = 0;

    // Group by sub-chunk for batch updates.
    Dictionary<(int cx, int cy, int cz), List<BlockChangeEntry>> subChunkEntries = [];

    for (int x = minX; x <= maxX; x++) {
      for (int z = minZ; z <= maxZ; z++) {
        for (int y = minY; y <= maxY; y++) {
          dimension.SetPermutation(x, y, z, permutation, broadcast: false);
          filled++;

          int cx = x >> 4;
          int cy = y >> 4;
          int cz = z >> 4;
          var key = (cx, cy, cz);

          if (!subChunkEntries.TryGetValue(key, out List<BlockChangeEntry>? entries)) {
            entries = [];
            subChunkEntries[key] = entries;
          }

          entries.Add(new BlockChangeEntry {
            Position = new BlockPos { X = x, Y = y, Z = z },
            BlockRuntimeId = (uint)permutation.NetworkId,
            Flags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network),
            SyncedUpdateEntityUniqueId = 0,
            SyncedUpdateType = 0
          });
        }
      }
    }

    foreach (((int scx, int scy, int scz), List<BlockChangeEntry> entries) in subChunkEntries) {
      UpdateSubChunkBlocksPacket packet = new() {
        SubChunkX = scx,
        SubChunkY = scy,
        SubChunkZ = scz,
        Blocks = entries
      };

      Vec3f center = new() {
        X = (scx << 4) + 8,
        Y = (scy << 4) + 8,
        Z = (scz << 4) + 8
      };

      dimension.Broadcast(packet, new BroadcastOptions {
        Radius = dimension.World?.Server?.Properties.MaxViewDistance * 16 ?? 256,
        Center = center
      });
    }

    return filled;
  }

  sealed class FillTask : ServerTask {
    private readonly Dimension _dimension;
    private readonly int _minX, _minY, _minZ;
    private readonly int _maxX, _maxY, _maxZ;
    private readonly BlockPermutation _permutation;
    private int _currentX, _currentY, _currentZ;
    private int _totalFilled;

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
      int count = 0;
      Dictionary<(int cx, int cy, int cz), List<BlockChangeEntry>> subChunkEntries = [];

      while (count < BlocksPerTick) {
        _dimension.SetPermutation(_currentX, _currentY, _currentZ, _permutation, broadcast: false);
        count++;
        _totalFilled++;

        int cx = _currentX >> 4;
        int cy = _currentY >> 4;
        int cz = _currentZ >> 4;
        var key = (cx, cy, cz);

        if (!subChunkEntries.TryGetValue(key, out List<BlockChangeEntry>? entries)) {
          entries = [];
          subChunkEntries[key] = entries;
        }

        entries.Add(new BlockChangeEntry {
          Position = new BlockPos { X = _currentX, Y = _currentY, Z = _currentZ },
          BlockRuntimeId = (uint)_permutation.NetworkId,
          Flags = (uint)(UpdateBlockFlagsType.Neighbors | UpdateBlockFlagsType.Network),
          SyncedUpdateEntityUniqueId = 0,
          SyncedUpdateType = 0
        });

        if (!Advance()) {
          BroadcastSubChunks(subChunkEntries);
          return;
        }
      }

      BroadcastSubChunks(subChunkEntries);
      _dimension.World?.Scheduler?.Schedule(this);
    }

    public override void Complete() {
      // Reset for potential reuse by scheduler.
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

    private void BroadcastSubChunks(Dictionary<(int cx, int cy, int cz), List<BlockChangeEntry>> subChunkEntries) {
      foreach (((int scx, int scy, int scz), List<BlockChangeEntry> entries) in subChunkEntries) {
        UpdateSubChunkBlocksPacket packet = new() {
          SubChunkX = scx,
          SubChunkY = scy,
          SubChunkZ = scz,
          Blocks = entries
        };

        Vec3f center = new() {
          X = (scx << 4) + 8,
          Y = (scy << 4) + 8,
          Z = (scz << 4) + 8
        };

        _dimension.Broadcast(packet, new BroadcastOptions {
          Radius = _dimension.World?.Server?.Properties.MaxViewDistance * 16 ?? 256,
          Center = center
        });
      }
    }
  }
}
