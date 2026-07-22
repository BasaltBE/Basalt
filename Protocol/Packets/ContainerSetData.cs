using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ContainerSetData)]
public sealed record ContainerSetDataPacket : DataPacket {
    public const int FurnaceTickCount = 0;
    public const int FurnaceLitTime = 1;
    public const int FurnaceLitDuration = 2;
    public const int FurnaceStoredXp = 3;
    public const int FurnaceFuelAux = 4;

    public ContainerId ContainerId;
    public int Property;
    public int Value;

    public override void Deserialize(Binary.BinaryReader reader) {
        ContainerId = (ContainerId)reader.ReadInt8();
        Property = reader.ReadZigZag();
        Value = reader.ReadZigZag();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteInt8((sbyte)ContainerId);
        writer.WriteZigZag(Property);
        writer.WriteZigZag(Value);
    }
}
