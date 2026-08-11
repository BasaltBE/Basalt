using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class server_config {
    public gatheringsConfig Gathering = new();
    public clientStoreEntryPointConfig ClientStoreEntryPoint = new();
    public presenceConfig Presence = new();

    public void Read(BinaryReader reader) {
        Gathering.Read(reader);
        ClientStoreEntryPoint.Read(reader);
        Presence.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        Gathering.Write(writer);
        ClientStoreEntryPoint.Write(writer);
        Presence.Write(writer);
    }
}
