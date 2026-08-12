#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CreativeItemEntryPayload {
    public CreativeItemNetId CreativeNetId = new();
    public NetworkItemInstanceDescriptorData ItemInstance = new();
    public uint GroupIndex;

    public void Read(BinaryReader reader) {
        CreativeNetId.Read(reader);
        ItemInstance.Read(reader);
        GroupIndex = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        CreativeNetId.Write(writer);
        ItemInstance.Write(writer);
        writer.WriteVarUInt(GroupIndex);
    }
}
