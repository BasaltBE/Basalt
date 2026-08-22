namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Enums;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;
using ChunkColumn = Worlds.Dimensions.Chunk.Chunk;

public class FarmlandTrait : BlockTrait {
    public static new readonly string Identifier = "minecraft:farmland";
    public static new readonly string[] Types = [BlockIdentifier.Farmland.ToIdentifier()];

    private const int WaterSearchRadius = 4;
    private const uint CheckIntervalMin = 8;
    private const uint CheckIntervalMax = 20;
    private const int DryChecksToDecay = 5;

    private static int? _waterHash;
    private static int? _flowingWaterHash;

    private int _dryTicks;
    private readonly int _dryChecksNeeded;

    public FarmlandTrait(Block block) : base(block) {
        _dryChecksNeeded = Random.Shared.Next(DryChecksToDecay, DryChecksToDecay + 4);
    }

    public override void OnPlace(BlockPlaceDetails details) {
        if (details.Player.Dimension is { } dimension)
            ScheduleFarmlandTick(dimension, details.BlockPosition);
    }

    public override void OnTick(BlockTickDetails details) {
        TickFarmland(details.Dimension, details.BlockPosition);
    }

    public override void OnLandOn(BlockLandOnDetails details) {
        base.OnLandOn(details);
        // TODO! Implement farmland decay on landing (1 in 2 chance iirc)
    }

    public static void ScheduleFarmlandTick(Dimension dimension, BlockPos pos, uint offset = 0) {
        uint delay = offset > 0 ? offset : (uint)Random.Shared.Next((int)CheckIntervalMin, (int)CheckIntervalMax + 1);
        dimension.ScheduleBlockTick(pos, delay);
    }

    private static (int Water, int FlowingWater) GetWaterHashes() {
        if (_waterHash is null) {
            BlockPermutation? waterPerm = BlockPermutation.Resolve(BlockIdentifier.Water.ToIdentifier());
            BlockPermutation? flowingPerm = BlockPermutation.Resolve(BlockIdentifier.FlowingWater.ToIdentifier());
            _waterHash = waterPerm?.NetworkId ?? 0;
            _flowingWaterHash = flowingPerm?.NetworkId ?? 0;
        }
        return (_waterHash!.Value, _flowingWaterHash!.Value);
    }

    private static int MoistureLevel(BlockPermutation perm) {
        if (!perm.State.TryGetValue("moisturized_amount", out BlockStateValue val))
            return 0;
        return val.Kind == 0 ? (int)val.AsNumber() : 0;
    }

    private static BlockPermutation? FindFarmlandPermutation(int moisture) {
        BlockType? bt = BlockType.Get(BlockIdentifier.Farmland.ToIdentifier());
        if (bt is null) return null;

        BlockState state = [];
        state["moisturized_amount"] = moisture;
        return bt.GetPermutation(state);
    }

    private static bool SearchForWater(Dimension dimension, int cx, int cy, int cz) {
        (int wh, int fwh) = GetWaterHashes();
        int r = WaterSearchRadius;

        int lastChunkX = int.MinValue;
        int lastChunkZ = int.MinValue;
        ChunkColumn? lastChunk = null;

        for (int dx = -r; dx <= r; dx++) {
            for (int dz = -r; dz <= r; dz++) {
                int bx = cx + dx;
                int bz = cz + dz;
                int cpx = bx >> 4;
                int cpz = bz >> 4;

                if (cpx != lastChunkX || cpz != lastChunkZ) {
                    lastChunkX = cpx;
                    lastChunkZ = cpz;
                    lastChunk = dimension.GetChunk(cpx, cpz);
                }

                if (lastChunk is null) continue;

                for (int dy = 0; dy <= 1; dy++) {
                    int localX = bx - (cpx * 16);
                    if (localX < 0) localX += 16;
                    int localZ = bz - (cpz * 16);
                    if (localZ < 0) localZ += 16;

                    BlockPermutation state = lastChunk.GetPermutation(localX, cy + dy, localZ, 0);
                    if (state.NetworkId == wh || state.NetworkId == fwh)
                        return true;
                }
            }
        }

        return false;
    }

    private static void TickFarmland(Dimension dimension, BlockPos pos) {
        BlockPermutation? perm;
        try { perm = dimension.GetPermutation(pos.X, pos.Y, pos.Z, 0); }
        catch { return; }

        if (!string.Equals(perm.Type.Identifier, BlockIdentifier.Farmland.ToIdentifier(), StringComparison.Ordinal))
            return;

        Block? block = dimension.GetBlock(pos.X, pos.Y, pos.Z);
        FarmlandTrait? trait = block?.GetTrait<FarmlandTrait>();

        if (SearchForWater(dimension, pos.X, pos.Y, pos.Z)) {
            if (trait is not null) trait._dryTicks = 0;

            int currentMoisture = MoistureLevel(perm);
            if (currentMoisture != 7) {
                BlockPermutation? moistPerm = FindFarmlandPermutation(7);
                if (moistPerm is not null)
                    dimension.SetPermutation(pos.X, pos.Y, pos.Z, moistPerm, 0, true);
            }
        }
        else {
            int dryCount = (trait?._dryTicks ?? 0) + 1;
            int threshold = trait?._dryChecksNeeded ?? DryChecksToDecay;

            if (dryCount >= threshold) {
                if (trait is not null) trait._dryTicks = 0;

                int currentMoisture = MoistureLevel(perm);
                if (currentMoisture > 0) {
                    BlockPermutation? dryPerm = FindFarmlandPermutation(0);
                    if (dryPerm is not null)
                        dimension.SetPermutation(pos.X, pos.Y, pos.Z, dryPerm, 0, true);
                }
                else {
                    BlockPermutation? dirtPerm = BlockPermutation.Resolve(BlockIdentifier.Dirt.ToIdentifier());
                    if (dirtPerm is not null) {
                        BlockPos cropPosition = new() {
                            X = pos.X,
                            Y = pos.Y + 1,
                            Z = pos.Z
                        };
                        Block? crop = dimension.GetBlock(
                            cropPosition.X,
                            cropPosition.Y,
                            cropPosition.Z);
                        BlockPermutation? airPerm = BlockPermutation.Resolve(BlockIdentifier.Air.ToIdentifier());
                        if (crop?.GetTrait<CropTrait>() is not null && airPerm is not null) {
                            dimension.Broadcast(new LevelEventPacket {
                                EventId = (int)LevelEvent.ParticlesDestroyBlock,
                                Position = new Vec3 {
                                    X = cropPosition.X + 0.5f,
                                    Y = cropPosition.Y + 0.5f,
                                    Z = cropPosition.Z + 0.5f
                                },
                                Data = crop.Permutation.NetworkId
                            });

                            ulong currentTick = dimension.World is Tickable tickable
                                ? tickable.TickValue
                                : 0;
                            foreach (Item.ItemStack drop in crop.GetDrops()) {
                                ItemEntity dropEntity = new(drop) {
                                    Position = new Vec3 {
                                        X = cropPosition.X + 0.5f,
                                        Y = cropPosition.Y + 0.5f,
                                        Z = cropPosition.Z + 0.5f
                                    }
                                };
                                float angle = Random.Shared.NextSingle() * MathF.Tau;
                                float speed = 0.07f + Random.Shared.NextSingle() * 0.06f;
                                dropEntity.Velocity = new Vec3 {
                                    X = MathF.Cos(angle) * speed,
                                    Y = 0.16f + Random.Shared.NextSingle() * 0.08f,
                                    Z = MathF.Sin(angle) * speed
                                };
                                dropEntity.LockPickupUntil(currentTick + 10);
                                dropEntity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
                            }

                            dimension.RemoveBlock(
                                cropPosition.X,
                                cropPosition.Y,
                                cropPosition.Z);
                            dimension.SetPermutation(
                                cropPosition.X,
                                cropPosition.Y,
                                cropPosition.Z,
                                airPerm,
                                0,
                                true);
                        }

                        dimension.RemoveBlock(pos.X, pos.Y, pos.Z);
                        dimension.SetPermutation(pos.X, pos.Y, pos.Z, dirtPerm, 0, true);
                    }
                    return;
                }
            }
            else {
                if (trait is not null) trait._dryTicks = dryCount;
            }
        }

        ScheduleFarmlandTick(dimension, pos);
    }

}
