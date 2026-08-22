using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(113)]
public sealed class SetLocalPlayerAsInitializedPacket : DataPacket {
    public ulong PlayerId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarULong(PlayerId);
    }

    public override void Deserialize(ref BinaryReader reader) {
        PlayerId = reader.ReadVarULong();
    }
}
