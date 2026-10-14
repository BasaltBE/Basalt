using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(46)]
public sealed class ContainerOpenPacket : DataPacket {
    public byte ContainerId;
    public byte ContainerType;
    public BlockPos Position = new();
    public long TargetActorId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8(ContainerId);
        writer.WriteUInt8(ContainerType);
        Position.Write(ref writer);
        writer.WriteZigZong(TargetActorId);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        Position.Read(ref reader);
        TargetActorId = reader.ReadZigZong();
    }
}
