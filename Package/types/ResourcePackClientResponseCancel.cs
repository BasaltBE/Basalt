#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ResourcePackClientResponseCancel : ResourcePackClientResponseVariant {
    public ResourcePackResponse ResponseType = global::BedrockProtocol.Enums.ResourcePackResponse.Cancel;

    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ResourcePackResponse constValue0 = (global::BedrockProtocol.Enums.ResourcePackResponse)reader.ReadInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ResourcePackResponse.Cancel) {
            throw new FormatException($"Expected cancel for ResponseType, got {constValue0}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt8((sbyte)global::BedrockProtocol.Enums.ResourcePackResponse.Cancel);
    }

    #pragma warning restore CA1822
}
