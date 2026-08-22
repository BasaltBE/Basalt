using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(10)]
public sealed class SetTimePacket : DataPacket {
    public int Time;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteZigZong(Time);
    public override void Deserialize(ref BinaryReader reader) => Time = checked((int)reader.ReadZigZong());
}
