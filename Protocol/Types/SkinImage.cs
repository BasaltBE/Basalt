using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SkinImage : DataType {
    /// <summary>The image width in pixels.</summary>
    public uint Width;

    /// <summary>The image height in pixels.</summary>
    public uint Height;

    /// <summary>The raw RGBA image data.</summary>
    public byte[] Data = [];

    public void Read(BinaryReader reader) {
        Width = reader.ReadUInt32(true);
        Height = reader.ReadUInt32(true);
        int length = checked((int)reader.ReadVarUInt());
        Data = reader.ReadBytes(length).ToArray();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(Width, true);
        writer.WriteUInt32(Height, true);
        writer.WriteVarUInt((uint)Data.Length);
        writer.WriteBytes(Data);
    }
}
