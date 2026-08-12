#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class InventoryAction {
    public InventorySource Source = new();
    public uint Slot;
    public NetworkItemStackDescriptor FromItem = new();
    public NetworkItemStackDescriptor ToItem = new();

    public void Read(BinaryReader reader) {
        Source.Read(reader);
        Slot = reader.ReadVarUInt();
        FromItem.Read(reader);
        ToItem.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Source.Write(writer);
        writer.WriteVarUInt(Slot);
        FromItem.Write(writer);
        ToItem.Write(writer);
    }
}
