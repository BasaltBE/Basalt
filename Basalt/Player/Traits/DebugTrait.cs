namespace Basalt.Core.Player.Traits;

using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Worlds;

using Entity = Basalt.Core.Entities.Entity;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Traits;
using Basalt.Core.Entities;

using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public sealed class DebugTrait : PlayerTrait {
    private const double TargetTps = 20.0;
    private const ulong SendIntervalTicks = 20;

    public new static string Identifier => "debug";
    public new static readonly EntityIdentifier[] Types = [
        EntityIdentifier.Player
    ];

    private ulong _lastSentTick;
    private double _averageMspt;

    public DebugTrait(Entity entity) : base(entity) {
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        _lastSentTick =
            Player.Dimension?.World is Tickable tickable
                ? tickable.TickValue
                : 0;

        _averageMspt = 0;
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (
            !Player.IsAlive ||
            details.CurrentTick - _lastSentTick < SendIntervalTicks
        ) {
            return;
        }

        try {
            double tps =
                Player.Dimension?.World?.Server?.Tps
                ?? TargetTps;

            double mspt =
                Player.Dimension?.World?.Server?.TickWork
                ?? 0;

            _averageMspt =
                _averageMspt == 0
                    ? mspt
                    : _averageMspt + ((mspt - _averageMspt) * 0.2);

            double workingSetMb =
                Environment.WorkingSet / (1024.0 * 1024.0);

            int chunksLoaded =
                Player.Dimension?.ChunkCount
                ?? 0;

            string message =
                $"§aTPS: §f{tps:0.0}§8/§f{TargetTps:0.0} " +
                $"§8| §aMSPT: §f{mspt:0.00} " +
                $"§8| §aA/MSPT: §f{_averageMspt:0.00} " +
                $"§8| §aRAM: §f{workingSetMb:0.0}MB " +
                $"§8| §aChunks: §f{chunksLoaded}";

            TextPacket packet = new() {
                Body = new MessageOnly {
                    MessageType = TextPacketType.tip,
                    Message = message
                },
                Localize = false,
                FilteredMessage = null
            };

            if (Player.Spawned)
                Player.Send(packet);

            _lastSentTick = details.CurrentTick;
        }
        catch (Exception exception) {
            Logger.Warn(
                $"[{Player.Username}] DebugTrait exception: {exception}"
            );
        }
    }

    public override EntityTrait Clone(Entity entity) {
        return new DebugTrait(entity);
    }
}
