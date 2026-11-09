using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Client.Network.Handlers;

internal sealed class ResourcePacksInfoHandler(BedrockClient client) {
    internal void Handle(ResourcePacksInfoPacket packet) => client.QueuePacket(new ResourcePackClientResponsePacket {
        Response = new ResourcePackClientResponseDownloadingFinished()
    });
}
