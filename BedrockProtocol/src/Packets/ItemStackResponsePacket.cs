using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(148)]
public sealed class ItemStackResponsePacket : DataPacket {
    public ItemStackResponseInfo[] Responses = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Responses.Length);
        for (int i = 0; i < Responses.Length; i++) Responses[i].Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Responses = new ItemStackResponseInfo[count];
        for (int i = 0; i < count; i++) {
            Responses[i] = new ItemStackResponseInfo();
            Responses[i].Read(ref reader);
        }
    }
}
