#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CreativeGroupInfoPayload {
    public CreativeItemCategory CreativeCategory;
    public string Name = string.Empty;
    public NetworkItemInstanceDescriptorData GroupIconItem = new();

    public void Read(BinaryReader reader) {
        CreativeCategory = (global::BedrockProtocol.Enums.CreativeItemCategory)reader.ReadUInt8();
        Name = reader.ReadVarString();
        GroupIconItem.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)CreativeCategory);
        writer.WriteVarString(Name);
        GroupIconItem.Write(writer);
    }
}
