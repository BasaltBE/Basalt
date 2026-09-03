using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SkinImage : DataType {
    public uint Width;
    public uint Height;
    public byte[] ImageBytes = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32(Width, true);
        writer.WriteUInt32(Height, true);
        writer.WriteVarUInt((uint)ImageBytes.Length);
        writer.WriteBytes(ImageBytes);
    }

    public override void Read(ref BinaryReader reader) {
        Width = reader.ReadUInt32(true);
        Height = reader.ReadUInt32(true);
        ImageBytes = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }
}
