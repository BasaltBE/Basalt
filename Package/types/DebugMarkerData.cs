#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DebugMarkerData {
    public string Text = string.Empty;
    public Vec3 Position = new();
    public Color Color = new();
    public ulong Duration;

    public void Read(BinaryReader reader) {
        Text = reader.ReadVarString();
        Position.Read(reader);
        Color.Read(reader);
        Duration = reader.ReadUInt64(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Text);
        Position.Write(writer);
        Color.Write(writer);
        writer.WriteUInt64(Duration, true);
    }
}
