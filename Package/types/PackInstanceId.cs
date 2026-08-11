using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PackInstanceId {
    public string PackID = string.Empty;
    public string Version = string.Empty;
    public string SubPackName = string.Empty;

    public void Read(BinaryReader reader) {
        PackID = reader.ReadVarString();
        Version = reader.ReadVarString();
        SubPackName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(PackID);
        writer.WriteVarString(Version);
        writer.WriteVarString(SubPackName);
    }
}
