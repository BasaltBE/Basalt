using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializableCells : DataType {
    public byte XSize;
    public byte YSize;
    public byte ZSize;
    public byte[] Storage = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8(XSize);
        writer.WriteUInt8(YSize);
        writer.WriteUInt8(ZSize);
        writer.WriteVarUInt((uint)Storage.Length);
        writer.WriteBytes(Storage);
    }

    public override void Read(ref BinaryReader reader) {
        XSize = reader.ReadUInt8();
        YSize = reader.ReadUInt8();
        ZSize = reader.ReadUInt8();
        int count = checked((int)reader.ReadVarUInt());
        Storage = reader.ReadBytes(count).ToArray();
    }
}
