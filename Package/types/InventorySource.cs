#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class InventorySource {
    public InventorySourceType SourceType;
    public sbyte? ContainerID;
    public InventorySourceFlags? BitFlags;

    public void Read(BinaryReader reader) {
        SourceType = (global::BedrockProtocol.Enums.InventorySourceType)reader.ReadVarUInt();
        if (reader.ReadBool()) {
            ContainerID = reader.ReadInt8();
        } else {
            ContainerID = default;
        }
        if (reader.ReadBool()) {
            BitFlags = (global::BedrockProtocol.Enums.InventorySourceFlags)reader.ReadVarUInt();
        } else {
            BitFlags = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt((uint)SourceType);
        writer.WriteBool(ContainerID is not null);
        if (ContainerID is { } optionalValue3) {
            writer.WriteInt8(optionalValue3);
        }
        writer.WriteBool(BitFlags is not null);
        if (BitFlags is { } optionalValue5) {
            writer.WriteVarUInt((uint)optionalValue5);
        }
    }
}
