using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class EnchantmentInstance : DataType {
    public EnchantType Type;
    public byte Level;

    public override void Read(ref BinaryReader reader) {
        Type = (EnchantType)reader.ReadUInt8();
        Level = reader.ReadUInt8();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)Type);
        writer.WriteUInt8(Level);
    }
}
