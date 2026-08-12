#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SubChunkPosOffset {
    public sbyte SubchunkOffsetX;
    public sbyte SubchunkOffsetY;
    public sbyte SubchunkOffsetZ;

    public void Read(BinaryReader reader) {
        SubchunkOffsetX = reader.ReadInt8();
        SubchunkOffsetY = reader.ReadInt8();
        SubchunkOffsetZ = reader.ReadInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt8(SubchunkOffsetX);
        writer.WriteInt8(SubchunkOffsetY);
        writer.WriteInt8(SubchunkOffsetZ);
    }
}
