using System.Buffers.Binary;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PackIdVersion : DataType {
    public Guid PackId;
    public string Version = string.Empty;

    public override void Read(ref BinaryReader reader) {
        Span<byte> id = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(id, reader.ReadUInt64(true));
        BinaryPrimitives.WriteUInt64BigEndian(id[8..], reader.ReadUInt64(true));
        PackId = new Guid(id, bigEndian: true);
        Version = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        Span<byte> id = stackalloc byte[16];
        PackId.TryWriteBytes(id, bigEndian: true, out _);
        writer.WriteUInt64(BinaryPrimitives.ReadUInt64BigEndian(id), true);
        writer.WriteUInt64(BinaryPrimitives.ReadUInt64BigEndian(id[8..]), true);
        writer.WriteVarString(Version);
    }
}
