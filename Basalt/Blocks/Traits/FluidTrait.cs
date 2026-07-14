namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

public enum FluidKind
{
    Water,
    Lava
}

public class FluidTrait : BlockTrait
{
    public static new readonly string Identifier = "minecraft:fluid";
    public static new readonly string[] Types =
    [
        BlockIdentifier.Water.ToIdentifier(),
        BlockIdentifier.FlowingWater.ToIdentifier(),
        BlockIdentifier.Lava.ToIdentifier(),
        BlockIdentifier.FlowingLava.ToIdentifier()
    ];

    public FluidTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        if (details.Player.Dimension is not { } dimension) return;

        FluidKind? kind = GetFluidKind(Block.Permutation);
        if (kind.HasValue)
        {
            ScheduleFluidTick(dimension, details.BlockPosition, kind.Value);
        }
    }

    public override void OnTick(BlockTickDetails details)
    {
    }

    public override void OnBreak(BlockBreakDetails details)
    {
        if (details.Player.Dimension is not { } dimension) return;

        FluidKind? kind = GetFluidKind(Block.Permutation);
        if (kind.HasValue)
        {
            NotifyFluidNeighbors(kind.Value, dimension, details.BlockPosition);
        }
    }

    public static void ScheduleFluidTick(Dimension dimension, BlockPos pos, FluidKind kind)
    {
        Server? server = dimension.World?.Server;
        if (server is null) return;

        uint delay = TickDelay(kind);
        server.Scheduler.Schedule(new FluidTickTask(dimension, pos, kind) { DelayTicks = delay, RunOnMainThread = true },
            dimension.World!.TickValue);
    }

    private static uint TickDelay(FluidKind kind) => kind switch
    {
        FluidKind.Water => 5,
        FluidKind.Lava => 30,
        _ => 5
    };

    private static int DropOff(FluidKind kind) => kind switch
    {
        FluidKind.Water => 1,
        FluidKind.Lava => 2,
        _ => 1
    };

    private static int MaxSpread(FluidKind kind) => kind switch
    {
        FluidKind.Water => 4,
        FluidKind.Lava => 2,
        _ => 4
    };

    private static int? LiquidDepth(BlockPermutation perm)
    {
        if (!perm.State.TryGetValue("liquid_depth", out BlockStateValue val))
            return null;
        return val.Kind == 0 ? (int)val.AsNumber() : null;
    }

    private static bool IsSourceBlock(FluidKind kind, BlockPermutation perm)
    {
        if (!perm.Type.Liquid) return false;
        string sid = kind == FluidKind.Water
            ? BlockIdentifier.Water.ToIdentifier()
            : BlockIdentifier.Lava.ToIdentifier();
        return string.Equals(perm.Type.Identifier, sid, StringComparison.Ordinal)
            && (LiquidDepth(perm) ?? -1) == 0;
    }

    private static bool IsFluid(FluidKind kind, BlockPermutation perm)
    {
        if (!perm.Type.Liquid) return false;
        return kind switch
        {
            FluidKind.Water => string.Equals(perm.Type.Identifier, BlockIdentifier.Water.ToIdentifier(), StringComparison.Ordinal)
                || string.Equals(perm.Type.Identifier, BlockIdentifier.FlowingWater.ToIdentifier(), StringComparison.Ordinal),
            FluidKind.Lava => string.Equals(perm.Type.Identifier, BlockIdentifier.Lava.ToIdentifier(), StringComparison.Ordinal)
                || string.Equals(perm.Type.Identifier, BlockIdentifier.FlowingLava.ToIdentifier(), StringComparison.Ordinal),
            _ => false
        };
    }

    private static BlockPermutation? GetBlock(Dimension dimension, BlockPos pos)
    {
        try { return dimension.GetPermutation(pos.X, pos.Y, pos.Z, 0); }
        catch { return null; }
    }

    public static bool IsReplaceable(Dimension dimension, BlockPos pos)
    {
        BlockPermutation? p = GetBlock(dimension, pos);
        if (p is null) return false;
        return p.Type.Air || p.Type.Liquid;
    }

    public static bool IsSolidBlock(Dimension dimension, BlockPos pos)
    {
        BlockPermutation? p = GetBlock(dimension, pos);
        if (p is null) return false;
        return p.Type.Solid;
    }

    private static (BlockPos Pos, BlockPermutation Perm)? TouchingWater(Dimension dimension, BlockPos pos)
    {
        ReadOnlySpan<(int dx, int dy, int dz)> offsets =
        [
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        ];

        foreach ((int dx, int dy, int dz) in offsets)
        {
            BlockPos neighbor = new() { X = pos.X + dx, Y = pos.Y + dy, Z = pos.Z + dz };
            BlockPermutation? perm = GetBlock(dimension, neighbor);
            if (perm is null) continue;
            if (IsFluid(FluidKind.Water, perm))
                return (neighbor, perm);
        }
        return null;
    }

    public static BlockPermutation? FlowingPerm(FluidKind kind, int depth)
    {
        string id = kind == FluidKind.Water
            ? BlockIdentifier.FlowingWater.ToIdentifier()
            : BlockIdentifier.FlowingLava.ToIdentifier();

        BlockType? bt = BlockType.Get(id);
        if (bt is null) return null;

        BlockState state = [];
        state["liquid_depth"] = depth;
        return bt.GetPermutation(state);
    }

    public static BlockPermutation? SourcePerm(FluidKind kind)
    {
        return BlockPermutation.Resolve(kind == FluidKind.Water
            ? BlockIdentifier.Water.ToIdentifier()
            : BlockIdentifier.Lava.ToIdentifier());
    }

    public static FluidKind? GetFluidKind(BlockPermutation perm)
    {
        if (!perm.Type.Liquid) return null;
        string id = perm.Type.Identifier;
        if (string.Equals(id, BlockIdentifier.Water.ToIdentifier(), StringComparison.Ordinal)
            || string.Equals(id, BlockIdentifier.FlowingWater.ToIdentifier(), StringComparison.Ordinal))
            return FluidKind.Water;
        if (string.Equals(id, BlockIdentifier.Lava.ToIdentifier(), StringComparison.Ordinal)
            || string.Equals(id, BlockIdentifier.FlowingLava.ToIdentifier(), StringComparison.Ordinal))
            return FluidKind.Lava;
        return null;
    }

    public static void TickFluid(FluidKind kind, Dimension dimension, int x, int y, int z)
    {
        BlockPos pos = new() { X = x, Y = y, Z = z };
        BlockPermutation? perm = GetBlock(dimension, pos);
        if (perm is null) { Logger.Warn($"[FluidTrait] TickFluid ({x},{y},{z}) perm is null"); return; }
        if (!IsFluid(kind, perm))
        {
            // Logger.Warn($"[FluidTrait] TickFluid ({x},{y},{z}) not fluid: {perm.Type.Identifier} liquid:{perm.Type.Liquid}");
            return;
        }

        int? depthOpt = LiquidDepth(perm);
        if (depthOpt is null) { Logger.Warn($"[FluidTrait] TickFluid ({x},{y},{z}) no liquid_depth on {perm.Type.Identifier}"); return; }
        int depth = depthOpt.Value;
        bool source = depth == 0;

        // Logger.Warn($"[FluidTrait] TickFluid ({x},{y},{z}) type: {perm.Type.Identifier} depth: {depth} source: {source}");

        if (kind == FluidKind.Lava && !source)
        {
            var water = TouchingWater(dimension, pos);
            if (water.HasValue)
            {
                BlockPermutation? cobble = BlockPermutation.Resolve(BlockIdentifier.Cobblestone.ToIdentifier());
                if (cobble is not null)
                {
                    FormBlock(dimension, pos, cobble);
                    return;
                }
            }
        }

        if (source)
        {
            DoSpread(kind, dimension, pos, 8);
            return;
        }

        int newAmt = ComputeAmount(kind, dimension, pos);
        if (newAmt <= 0)
        {
            BlockPermutation air = BlockPermutation.Resolve("minecraft:air");
            dimension.RemoveBlock(x, y, z);
            dimension.SetPermutation(pos.X, pos.Y, pos.Z, air);
            NotifyFluidNeighbors(kind, dimension, pos);
            return;
        }

        BlockPos abovePos = new() { X = x, Y = y + 1, Z = z };
        BlockPermutation? abovePerm = GetBlock(dimension, abovePos);
        bool fluidAbove = abovePerm is not null && IsFluid(kind, abovePerm);
        int newDepth = (newAmt == 8 && fluidAbove) ? 8 : (newAmt == 8) ? 0 : 8 - newAmt;

        if (newDepth != depth)
        {
            BlockPermutation? np = newDepth == 0 ? SourcePerm(kind) : FlowingPerm(kind, newDepth);
            if (np is not null)
                dimension.SetPermutation(pos.X, pos.Y, pos.Z, np);
        }

        DoSpread(kind, dimension, pos, newAmt);
    }

    private static int ComputeAmount(FluidKind kind, Dimension dimension, BlockPos pos)
    {
        BlockPos above = new() { X = pos.X, Y = pos.Y + 1, Z = pos.Z };
        BlockPermutation? ap = GetBlock(dimension, above);
        if (ap is not null && IsFluid(kind, ap)) return 8;

        BlockPermutation? currentPerm = GetBlock(dimension, pos);
        int currentAmt = 0;
        if (currentPerm is not null)
        {
            int? cd = LiquidDepth(currentPerm);
            if (cd.HasValue)
                currentAmt = cd.Value == 0 ? 8 : 8 - (cd.Value & 7);
        }

        int best = 0;
        int sources = 0;
        ReadOnlySpan<(int dx, int dz)> neighbors = [(1, 0), (-1, 0), (0, 1), (0, -1)];

        foreach ((int dx, int dz) in neighbors)
        {
            BlockPos nb = new() { X = pos.X + dx, Y = pos.Y, Z = pos.Z + dz };
            BlockPermutation? nbp = GetBlock(dimension, nb);
            if (nbp is null || !IsFluid(kind, nbp)) continue;

            int? d = LiquidDepth(nbp);
            if (d is null) continue;

            int amt = d.Value == 0 ? 8 : (d.Value & 7) == 0 ? 8 : 8 - (d.Value & 7);
            if (amt <= currentAmt) continue;
            if (amt > best) best = amt;
            if (IsSourceBlock(kind, nbp)) sources++;
        }

        if (sources >= 2)
        {
            BlockPos floor = new() { X = pos.X, Y = pos.Y - 1, Z = pos.Z };
            if (IsSolidBlock(dimension, floor)) return 8;
            BlockPermutation? fp = GetBlock(dimension, floor);
            if (fp is not null && IsFluid(kind, fp)) return 8;
        }

        return best - DropOff(kind);
    }

    private static void DoSpread(FluidKind kind, Dimension dimension, BlockPos pos, int amount)
    {
        BlockPermutation? perm = GetBlock(dimension, pos);
        if (perm is null) return;
        int? depthOpt = LiquidDepth(perm);
        if (depthOpt is null) return;
        int depth = depthOpt.Value;
        bool source = depth == 0;

        BlockPos below = new() { X = pos.X, Y = pos.Y - 1, Z = pos.Z };
        bool solidBelow = IsSolidBlock(dimension, below);

        if (!source && !solidBelow)
        {
            FlowDown(kind, dimension, pos);
            return;
        }

        FlowDown(kind, dimension, pos);

        bool falling = (depth & 8) != 0 && depth != 0;
        int spreadAmt = falling ? 7 : amount - DropOff(kind);
        if (spreadAmt <= 0) return;

        int spreadDepth = 8 - spreadAmt;
        FlowSideways(kind, dimension, pos, spreadDepth);
    }

    private static void FlowDown(FluidKind kind, Dimension dimension, BlockPos pos)
    {
        BlockPos below = new() { X = pos.X, Y = pos.Y - 1, Z = pos.Z };
        if (!IsReplaceable(dimension, below)) return;

        BlockPermutation? bp = GetBlock(dimension, below);
        if (bp is not null && IsFluid(kind, bp) && (LiquidDepth(bp) ?? 0) == 8) return;

        BlockPermutation? fp = FlowingPerm(kind, 8) ?? SourcePerm(kind);
        if (fp is null) return;
        PlaceFluid(kind, dimension, below, fp);
    }

    private static void FlowSideways(FluidKind kind, Dimension dimension, BlockPos pos, int spreadDepth)
    {
        ReadOnlySpan<(int dx, int dz)> dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];

        int minDist = 1000;
        Span<bool> targets = stackalloc bool[4];
        targets.Clear();

        for (int i = 0; i < dirs.Length; i++)
        {
            (int dx, int dz) = dirs[i];
            BlockPos nb = new() { X = pos.X + dx, Y = pos.Y, Z = pos.Z + dz };
            if (!IsReplaceable(dimension, nb)) continue;

            BlockPermutation? nbp = GetBlock(dimension, nb);
            if (nbp is not null && IsFluid(kind, nbp)) continue;

            BlockPos nbBelow = new() { X = nb.X, Y = nb.Y - 1, Z = nb.Z };
            bool isHole = IsReplaceable(dimension, nbBelow) && (GetBlock(dimension, nbBelow) is not { } fbp || !IsFluid(kind, fbp));

            int dist = isHole ? 0 : SlopeDist(kind, dimension, nb, 1, -dx, -dz, MaxSpread(kind));

            if (dist < minDist)
            {
                minDist = dist;
                targets.Clear();
            }
            if (dist <= minDist) targets[i] = true;
        }

        BlockPermutation? sp = FlowingPerm(kind, spreadDepth);
        if (sp is null) return;

        for (int i = 0; i < dirs.Length; i++)
        {
            if (!targets[i]) continue;
            (int dx, int dz) = dirs[i];
            BlockPos nb = new() { X = pos.X + dx, Y = pos.Y, Z = pos.Z + dz };
            PlaceFluid(kind, dimension, nb, sp);
        }
    }

    private static int SlopeDist(FluidKind kind, Dimension dimension, BlockPos pos, int pass, int fdx, int fdz, int max)
    {
        ReadOnlySpan<(int dx, int dz)> dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        int lowest = 1000;

        foreach ((int dx, int dz) in dirs)
        {
            if (dx == fdx && dz == fdz) continue;

            BlockPos next = new() { X = pos.X + dx, Y = pos.Y, Z = pos.Z + dz };
            if (!IsReplaceable(dimension, next)) continue;

            BlockPermutation? np = GetBlock(dimension, next);
            if (np is not null && IsFluid(kind, np)) continue;

            BlockPos nextBelow = new() { X = next.X, Y = next.Y - 1, Z = next.Z };
            bool hole = IsReplaceable(dimension, nextBelow) && (GetBlock(dimension, nextBelow) is not { } fbp || !IsFluid(kind, fbp));

            if (hole) return pass;
            if (pass < max)
            {
                int v = SlopeDist(kind, dimension, next, pass + 1, -dx, -dz, max);
                if (v < lowest) lowest = v;
            }
        }
        return lowest;
    }

    public static void PlaceFluid(FluidKind kind, Dimension dimension, BlockPos pos, BlockPermutation perm)
    {
        BlockPermutation? existing = GetBlock(dimension, pos);

        if (kind == FluidKind.Lava && existing is not null && IsFluid(FluidKind.Water, existing))
            return;

        if (kind == FluidKind.Water && existing is not null)
        {
            if (IsSourceBlock(FluidKind.Lava, existing))
            {
                BlockPermutation? obsidian = BlockPermutation.Resolve(BlockIdentifier.Obsidian.ToIdentifier());
                if (obsidian is not null)
                {
                    FormBlock(dimension, pos, obsidian);
                    return;
                }
            }
            if (IsFluid(FluidKind.Lava, existing))
            {
                BlockPermutation? cobble = BlockPermutation.Resolve(BlockIdentifier.Cobblestone.ToIdentifier());
                if (cobble is not null)
                {
                    FormBlock(dimension, pos, cobble);
                    return;
                }
            }
        }

        if (!IsReplaceable(dimension, pos)) return;

        if (kind == FluidKind.Lava)
        {
            var water = TouchingWater(dimension, pos);
            if (water.HasValue)
            {
                BlockPermutation? cobble = BlockPermutation.Resolve(BlockIdentifier.Cobblestone.ToIdentifier());
                if (cobble is not null)
                {
                    FormBlock(dimension, pos, cobble);
                    return;
                }
            }
        }

        dimension.RemoveBlock(pos.X, pos.Y, pos.Z);
        dimension.SetPermutation(pos.X, pos.Y, pos.Z, perm);
        ScheduleFluidTick(dimension, pos, kind);

        if (kind == FluidKind.Water)
        {
            NotifyNearbyFarmland(dimension, pos);
        }
    }

    private static void FormBlock(Dimension dimension, BlockPos pos, BlockPermutation newPermutation)
    {
        dimension.RemoveBlock(pos.X, pos.Y, pos.Z);
        dimension.SetPermutation(pos.X, pos.Y, pos.Z, newPermutation);
    }

    public static void NotifyFluidNeighbors(FluidKind kind, Dimension dimension, BlockPos pos)
    {
        ReadOnlySpan<(int dx, int dy, int dz)> offsets =
        [
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        ];

        int scheduled = 0;
        foreach ((int dx, int dy, int dz) in offsets)
        {
            BlockPos neighbor = new() { X = pos.X + dx, Y = pos.Y + dy, Z = pos.Z + dz };
            BlockPermutation? perm = GetBlock(dimension, neighbor);
            if (perm is not null && IsFluid(kind, perm))
            {
                ScheduleFluidTick(dimension, neighbor, kind);
                scheduled++;
            }
        }
        // Logger.Warn($"[FluidTrait] NotifyFluidNeighbors at ({pos.X},{pos.Y},{pos.Z}) scheduled {scheduled} ticks");

        if (kind == FluidKind.Water)
        {
            NotifyNearbyFarmland(dimension, pos);
        }
    }

    private static void NotifyNearbyFarmland(Dimension dimension, BlockPos pos)
    {
        int radius = 4; // Match FarmlandTrait.WaterSearchRadius
        string farmlandId = BlockIdentifier.Farmland.ToIdentifier();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dy = -1; dy <= 0; dy++)
                {
                    int bx = pos.X + dx;
                    int by = pos.Y + dy;
                    int bz = pos.Z + dz;

                    BlockPermutation? perm;
                    try { perm = dimension.GetPermutation(bx, by, bz, 0); }
                    catch { continue; }

                    if (string.Equals(perm.Type.Identifier, farmlandId, StringComparison.Ordinal))
                    {
                        BlockPos farmPos = new() { X = bx, Y = by, Z = bz };
                        uint delay = (uint)Random.Shared.Next(20, 61);
                        FarmlandTrait.ScheduleFarmlandTick(dimension, farmPos, offset: delay);
                    }
                }
            }
        }
    }

    private sealed class FluidTickTask : DelayedTask
    {
        private readonly Dimension _dimension;
        private readonly BlockPos _pos;
        private readonly FluidKind _kind;

        public FluidTickTask(Dimension dimension, BlockPos pos, FluidKind kind)
        {
            _dimension = dimension;
            _pos = pos;
            _kind = kind;
            RunOnMainThread = true;
        }

        public override void Execute()
        {
            // Logger.Warn($"[FluidTickTask] Executing at ({_pos.X},{_pos.Y},{_pos.Z}) kind:{_kind}");
            TickFluid(_kind, _dimension, _pos.X, _pos.Y, _pos.Z);
        }
    }
}
