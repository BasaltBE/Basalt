using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SerializableVoxelCells : DataType {
    public byte XSize;
    public byte YSize;
    public byte ZSize;
    public byte[] Storage = [];

    public override void Read(ref BinaryReader reader) {
        XSize = reader.ReadUInt8();
        YSize = reader.ReadUInt8();
        ZSize = reader.ReadUInt8();
        uint storageCount = reader.ReadVarUInt();
        if (XSize > 127 || YSize > 127 || ZSize > 127 || storageCount > 256_048)
            throw new InvalidDataException("Voxel cell dimensions or storage count are invalid.");
        Storage = reader.ReadBytes((int)storageCount).ToArray();
    }

    public override void Write(ref BinaryWriter writer) {
        if (XSize > 127 || YSize > 127 || ZSize > 127 || Storage.Length > 256_048)
            throw new InvalidDataException("Voxel cell dimensions or storage count are invalid.");

        writer.WriteUInt8(XSize);
        writer.WriteUInt8(YSize);
        writer.WriteUInt8(ZSize);
        writer.WriteVarUInt((uint)Storage.Length);
        writer.WriteBytes(Storage);
    }
}
