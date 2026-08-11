using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestSlotInfo {
    public FullContainerName FullContainerName = new();
    public byte Slot;
    public int NetIdVariant;

    public void Read(BinaryReader reader) {
        FullContainerName.Read(reader);
        Slot = reader.ReadUInt8();
        NetIdVariant = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        FullContainerName.Write(writer);
        writer.WriteUInt8(Slot);
        writer.WriteInt32(NetIdVariant, true);
    }
}
