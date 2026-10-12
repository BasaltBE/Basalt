using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class PlayStatusHandler(BedrockClient client) {
    internal void Handle(PlayStatusPacket packet) {
        switch (packet.Status) {
            case PlayStatus.LoginSuccess:
                client.QueuePacket(new ClientCacheStatusPacket());
                break;
            case PlayStatus.PlayerSpawn:
                client.QueuePacket(new SetLocalPlayerAsInitializedPacket { RuntimeId = client.StartGame.RuntimeId });
                client.Spawned = true;
                client.SpawnCompletion.TrySetResult();
                break;
            default:
                throw new InvalidDataException($"Bedrock login failed with status {packet.Status}.");
        }
    }
}
