using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(79)]
public sealed class CommandOutputPacket : DataPacket {
    public CommandOriginData Origin = new();
    public CommandOutput Output = new();

    public override void Deserialize(ref BinaryReader reader) {
        Origin.Read(ref reader);
        Output.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        Origin.Write(ref writer);
        Output.Write(ref writer);
    }
}
