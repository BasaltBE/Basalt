namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Player.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Worlds;

using Basalt.BedrockProtocol.Packets;

public static class SetLocalPlayerAsInitialized {
    public static void Handle(Server server, NetworkConnection connection, SetLocalPlayerAsInitializedPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            Logger.Warn("SetLocalPlayerAsInitialized received for unknown player session.");
            return;
        }
        ulong tick = player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;

        Logger.Info($"Local player initialized: unique={player.UniqueId}, runtime={player.RuntimeId}, tick={tick}");

        player.Attributes.Send(true);

        server.Network.QueuePacket(connection, player.CreateActorDataPacket(tick));

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is not null) {
            chunkRendering.StartChunkLoad();
        }

        player.Dimension?.AddPlayer(player);

        DebugTrait? debugTrait = player.GetTrait<DebugTrait>();
        if (debugTrait is null) {
            debugTrait = player.AddTrait(new DebugTrait(player));
            debugTrait.OnSpawn(new EntitySpawnOptions(InitialSpawn: false));
        }

        player.Spawned = true;
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is not null) {
            inventory.Container.Update();
        }

        EntityEquipmentTrait? equipment = player.GetTrait<EntityEquipmentTrait>();
        if (equipment is not null) {
            equipment.SyncToPlayer(player);
        }

        player.AttributesDirty = true;

        string joinMessage = $"§e{player.Username} joined the server.";
        foreach (Player.Player target in server.Players.Values) {
            // target.SendMessage(joinMessage);
        }

        Logger.Info($"Player {player.Username} has spawned.");
    }
}










