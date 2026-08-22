using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SlotInfoData : DataType {
    public FullContainerName Container = new();
    public byte Slot;
    public int NetIdVariant;

    public override void Write(ref BinaryWriter writer) {
        Container.Write(ref writer);
        writer.WriteUInt8(Slot);
        writer.WriteInt32(NetIdVariant, true);
    }

    public override void Read(ref BinaryReader reader) {
        Container.Read(ref reader);
        Slot = reader.ReadUInt8();
        NetIdVariant = reader.ReadInt32(true);
    }
}
