using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ServerboundDataStore)]
public sealed record ServerboundDataStorePacket : DataPacket {
    public DataStoreUpdate Update = new();

    public override void Deserialize(Binary.BinaryReader reader) {
        Update.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        Update.Write(writer);
    }
}
