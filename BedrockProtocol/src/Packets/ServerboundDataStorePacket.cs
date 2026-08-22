using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(332)]
public sealed class ServerboundDataStorePacket : DataPacket {
    public DataStoreUpdate Update = new();

    public override void Serialize(ref BinaryWriter writer) {
        Update.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Update.Read(ref reader);
    }
}
