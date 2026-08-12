#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SubChunkPos {
    public int SubchunkPositionX;
    public int SubchunkPositionY;
    public int SubchunkPositionZ;

    public void Read(BinaryReader reader) {
        SubchunkPositionX = reader.ReadInt32(true);
        SubchunkPositionY = reader.ReadInt32(true);
        SubchunkPositionZ = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(SubchunkPositionX, true);
        writer.WriteInt32(SubchunkPositionY, true);
        writer.WriteInt32(SubchunkPositionZ, true);
    }
}
