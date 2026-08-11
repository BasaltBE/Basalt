using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ResourcePackClientResponseDownloading : ResourcePackClientResponseVariant {
    public ResourcePackResponse ResponseType = global::BedrockProtocol.Enums.ResourcePackResponse.Downloading;
    public List<string> DownloadingPacks = [];

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ResourcePackResponse constValue0 = (global::BedrockProtocol.Enums.ResourcePackResponse)reader.ReadInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ResourcePackResponse.Downloading) {
            throw new FormatException($"Expected downloading for ResponseType, got {constValue0}.");
        }
        DownloadingPacks = new List<string>();
        while (reader.Remaining > 0) {
            string item2 = default!;
            item2 = reader.ReadVarString();
            DownloadingPacks.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt8((sbyte)global::BedrockProtocol.Enums.ResourcePackResponse.Downloading);
        foreach (var item3 in DownloadingPacks) {
            writer.WriteVarString(item3);
        }
    }
}
