using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PackIdVersion {
    public UUID PackUUID = new();
    public SemVersion PackVersion = new();

    public void Read(BinaryReader reader) {
        PackUUID.Read(reader);
        PackVersion.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        PackUUID.Write(writer);
        PackVersion.Write(writer);
    }
}
