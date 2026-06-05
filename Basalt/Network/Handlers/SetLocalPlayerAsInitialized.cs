namespace Basalt.Server.Network.Handlers;

using Basalt.Server;
using Basalt.Server.Entity.Traits;
using Basalt.Server.Player.Traits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.Server.Traits;
using Basalt.Server.Entity.Traits.Types;
using Basalt.Server.World;


public static class SetLocalPlayerAsInitialized
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        SetLocalPlayerAsInitializedPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (SetLocalPlayerAsInitializedPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out global::Basalt.Server.Player.Player? player))
        {
            Logger.Warn("SetLocalPlayerAsInitialized received for unknown player session.");
            return;
        }
        ulong tick = player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;

        server.Network.SendPacket(connection, player.CreateActorDataPacket(tick));
        player.SendAttributes();

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is not null)
        {
            chunkRendering.StartChunkLoad();
        }

        DebugTrait? debugTrait = player.GetTrait<DebugTrait>();
        if (debugTrait is null)
        {
            debugTrait = player.AddTrait(new DebugTrait(player));
            debugTrait.OnSpawn(new EntitySpawnOptions(InitialSpawn: false));
        }

        player.SetSpawned(true);

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is not null)
        {
            inventory.Container.Update();
        }

        string joinMessage = $"§e{player.Username} joined the server.";
        foreach (global::Basalt.Server.Player.Player target in server.Players.Values)
        {
            // target.SendMessage(joinMessage);
        }

        Logger.Info($"Player {player.Username} has spawned.");
    }
}










