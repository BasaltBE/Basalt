using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(340)]
public sealed class ResourcePacksReadyForValidationPacket : DataPacket {
    public override void Serialize(ref BinaryWriter writer) {
    }

    public override void Deserialize(ref BinaryReader reader) {
    }
}
