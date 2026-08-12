#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class FullContainerName {
    public ContainerEnumName ContainerName;
    public uint? DynamicID;

    public void Read(BinaryReader reader) {
        ContainerName = (global::BedrockProtocol.Enums.ContainerEnumName)reader.ReadUInt8();
        if (reader.ReadBool()) {
            DynamicID = reader.ReadUInt32(true);
        } else {
            DynamicID = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)ContainerName);
        writer.WriteBool(DynamicID is not null);
        if (DynamicID is { } optionalValue3) {
            writer.WriteUInt32(optionalValue3, true);
        }
    }
}
