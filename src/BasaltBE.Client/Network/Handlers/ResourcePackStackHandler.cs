using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class ResourcePackStackHandler(BedrockClient client) {
    internal void Handle(ResourcePackStackPacket packet) => client.QueuePacket(new ResourcePackClientResponsePacket {
        Response = new ResourcePackClientResponseStackFinished()
    });
}
