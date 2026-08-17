#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RemoveOverride : PlayerUpdateEntityOverridesUpdateVariant {
    public UpdateType Type = global::BedrockProtocol.Enums.UpdateType.RemoveOverride;

    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.UpdateType constValue0 = (global::BedrockProtocol.Enums.UpdateType)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.UpdateType.RemoveOverride) {
            throw new FormatException($"Expected removeoverride for Type, got {constValue0}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.UpdateType.RemoveOverride);
    }

    #pragma warning restore CA1822
}
