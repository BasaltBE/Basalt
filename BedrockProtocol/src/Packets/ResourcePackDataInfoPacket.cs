using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(82)]
public sealed class ResourcePackDataInfoPacket : DataPacket {
    public string ResourceName = string.Empty;
    public uint ChunkSize;
    public uint NumberOfChunks;
    public ulong FileSize;
    public string FileHash = string.Empty;
    public bool IsPremiumPack;
    public byte PackType;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(ResourceName);
        writer.WriteUInt32(ChunkSize, true);
        writer.WriteUInt32(NumberOfChunks, true);
        writer.WriteUInt64(FileSize, true);
        writer.WriteVarString(FileHash);
        writer.WriteBool(IsPremiumPack);
        writer.WriteUInt8(PackType);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourceName = reader.ReadVarString();
        ChunkSize = reader.ReadUInt32(true);
        NumberOfChunks = reader.ReadUInt32(true);
        FileSize = reader.ReadUInt64(true);
        FileHash = reader.ReadVarString();
        IsPremiumPack = reader.ReadBool();
        PackType = reader.ReadUInt8();
    }
}
