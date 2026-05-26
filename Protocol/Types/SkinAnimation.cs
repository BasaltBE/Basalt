using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SkinAnimation : DataType
{
    /// <summary>
    /// Animation image width.
    /// </summary>
    public uint ImageWidth;

    /// <summary>
    /// Animation image height.
    /// </summary>
    public uint ImageHeight;

    /// <summary>
    /// Animation image data.
    /// </summary>
    public byte[] ImageData = [];

    /// <summary>
    /// Animation type id.
    /// </summary>
    public uint AnimationType;

    /// <summary>
    /// Number of animation frames.
    /// </summary>
    public float FrameCount;

    /// <summary>
    /// Expression type id.
    /// </summary>
    public uint ExpressionType;

    public void Read(BinaryReader reader)
    {
        ImageWidth = reader.ReadUInt32(true);
        ImageHeight = reader.ReadUInt32(true);
        ImageData = ReadByteArray(reader);
        AnimationType = reader.ReadUInt32(true);
        FrameCount = reader.ReadF32(true);
        ExpressionType = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt32(ImageWidth, true);
        writer.WriteUInt32(ImageHeight, true);
        WriteByteArray(writer, ImageData);
        writer.WriteUInt32(AnimationType, true);
        writer.WriteF32(FrameCount, true);
        writer.WriteUInt32(ExpressionType, true);
    }

    internal static byte[] ReadByteArray(BinaryReader reader)
    {
        int length = checked((int)reader.ReadVarUInt());
        return reader.ReadBytes(length).ToArray();
    }

    internal static void WriteByteArray(BinaryWriter writer, byte[] value)
    {
        writer.WriteVarUInt((uint)value.Length);
        writer.WriteBytes(value);
    }
}
