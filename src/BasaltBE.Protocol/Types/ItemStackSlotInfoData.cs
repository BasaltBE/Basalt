using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemStackSlotInfoData : DataType {
    public FullContainerName ContainerName = new();
    public byte Slot;
    public int NetIdVariant;

    public override void Read(ref BinaryReader reader) {
        ContainerName.Read(ref reader);
        Slot = reader.ReadUInt8();
        NetIdVariant = reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        ContainerName.Write(ref writer);
        writer.WriteUInt8(Slot);
        writer.WriteInt32(NetIdVariant, true);
    }
}
