using BasaltBE.Protocol.Packets;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class StartGameHandler(BedrockClient client) {
    internal void Handle(StartGamePacket packet) {
        client.StartGame = packet;
        client.QueuePacket(new RequestChunkRadiusPacket { ChunkRadius = 8, MaxChunkRadius = 8 });
    }
}
