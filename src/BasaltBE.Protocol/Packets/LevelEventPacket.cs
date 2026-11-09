using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(25)]
public sealed class LevelEventPacket : DataPacket {
    public int EventId;
    public Vec3 Position = new();
    public int Data;

    public override void Deserialize(ref BinaryReader reader) {
        EventId = reader.ReadZigZag();
        Position.Read(ref reader);
        Data = reader.ReadZigZag();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag(EventId);
        Position.Write(ref writer);
        writer.WriteZigZag(Data);
    }
}
