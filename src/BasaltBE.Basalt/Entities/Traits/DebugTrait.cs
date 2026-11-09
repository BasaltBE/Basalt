using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class DebugTrait : EntityTrait {
    private const double TargetTps = 20;
    private const ulong SendIntervalTicks = 20;
    private readonly Player player;
    private ulong lastSentTick;
    private double averageMspt;

    public DebugTrait(Entity entity) : base(entity) => player = (Player)entity;

    public override void OnSpawn() {
        lastSentTick = player.Dimension.TickValue;
        averageMspt = 0;
    }

    public override void Tick() {
        if (!player.Spawned || player.Dead || player.Dimension.TickValue - lastSentTick < SendIntervalTicks) return;

        double tps = player.World.Ticks.Tps;
        double mspt = player.World.Ticks.LastMilliseconds;
        double controlTps = player.Network.Server.Ticks.Tps;
        double controlMspt = player.Network.Server.Ticks.LastMilliseconds;
        averageMspt = averageMspt == 0 ? mspt : averageMspt + (mspt - averageMspt) * 0.2;
        double memoryMiB = Environment.WorkingSet / (1024.0 * 1024.0);

        TextPacket packet = new() {
            Type = TextMessageType.Tip,
            Localize = false,
            Message = FormattableString.Invariant(
                $"§aTPS §f{tps:0.00}/{TargetTps:0.00} §8| §aMSPT §f{mspt:0.000} ms ({averageMspt:0.000} avg) §8| §aControl §f{controlTps:0.00} TPS/{controlMspt:0.000} ms §8| §aRAM §f{memoryMiB:0.0} MiB §8| §aChunks §f{player.Dimension.ChunkCount}"
            )
        };

        try {
            player.SendPacket(packet);
            lastSentTick = player.Dimension.TickValue;
        } catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
            player.Network.RemovePlayer(player.Peer);
        }
    }
}
