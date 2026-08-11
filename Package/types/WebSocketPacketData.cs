using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class WebSocketPacketData {
    public string WebsocketServerURI = string.Empty;

    public void Read(BinaryReader reader) {
        WebsocketServerURI = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(WebsocketServerURI);
    }
}
