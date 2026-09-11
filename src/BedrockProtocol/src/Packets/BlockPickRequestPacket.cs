using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(34)]
public sealed class BlockPickRequestPacket : DataPacket {
    public BlockPos Position = new();
    public bool WithData;
    public byte MaxSlots;

    public override void Serialize(ref BinaryWriter writer) {
        Position.Write(ref writer);
        writer.WriteBool(WithData);
        writer.WriteUInt8(MaxSlots);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        WithData = reader.ReadBool();
        MaxSlots = reader.ReadUInt8();
    }
}
