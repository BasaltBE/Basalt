#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class WaypointHandle {
    public UUID UUID = new();

    public void Read(BinaryReader reader) {
        UUID.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        UUID.Write(writer);
    }
}
