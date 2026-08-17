#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ClearOverride : PlayerUpdateEntityOverridesUpdateVariant {
    public UpdateType Type = global::BedrockProtocol.Enums.UpdateType.ClearOverrides;

    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.UpdateType constValue0 = (global::BedrockProtocol.Enums.UpdateType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.UpdateType.ClearOverrides) {
            throw new FormatException($"Expected clearoverrides for Type, got {constValue0}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.UpdateType.ClearOverrides);
    }

    #pragma warning restore CA1822
}
