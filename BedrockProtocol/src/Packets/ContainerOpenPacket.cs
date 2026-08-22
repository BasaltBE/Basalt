using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(46)]
public sealed class ContainerOpenPacket : DataPacket {
    public ContainerId ContainerId;
    public byte ContainerType;
    public BlockPos Position = new();
    public long TargetActorId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerId);
        writer.WriteUInt8(ContainerType);
        Position.Write(ref writer);
        writer.WriteVarLong(TargetActorId);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        Position.Read(ref reader);
        TargetActorId = reader.ReadVarLong();
    }
}
