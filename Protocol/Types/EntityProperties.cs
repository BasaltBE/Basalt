using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class EntityProperties : DataType {
    /// <summary>
    /// Integer-based entity properties.
    /// </summary>
    public List<IntegerEntityProperty> IntegerProperties = [];

    /// <summary>
    /// Float-based entity properties.
    /// </summary>
    public List<FloatEntityProperty> FloatProperties = [];

    public void Read(BinaryReader reader) {
        int integerPropertyCount = reader.ReadVarInt();
        IntegerProperties = new List<IntegerEntityProperty>(integerPropertyCount);
        for (int i = 0; i < integerPropertyCount; i++) {
            IntegerEntityProperty property = new();
            property.Read(reader);
            IntegerProperties.Add(property);
        }

        int floatPropertyCount = reader.ReadVarInt();
        FloatProperties = new List<FloatEntityProperty>(floatPropertyCount);
        for (int i = 0; i < floatPropertyCount; i++) {
            FloatEntityProperty property = new();
            property.Read(reader);
            FloatProperties.Add(property);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarInt(IntegerProperties.Count);
        for (int i = 0; i < IntegerProperties.Count; i++) {
            IntegerProperties[i].Write(writer);
        }

        writer.WriteVarInt(FloatProperties.Count);
        for (int i = 0; i < FloatProperties.Count; i++) {
            FloatProperties[i].Write(writer);
        }
    }
}
