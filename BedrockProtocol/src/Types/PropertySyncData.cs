using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PropertySyncData : DataType {
    public PropertySyncIntEntry[] IntEntries = [];
    public PropertySyncFloatEntry[] FloatEntries = [];

    public override void Write(ref BinaryWriter writer) {
        WriteArray(ref writer, IntEntries);
        WriteArray(ref writer, FloatEntries);
    }

    public override void Read(ref BinaryReader reader) {
        IntEntries = ReadArray<PropertySyncIntEntry>(ref reader);
        FloatEntries = ReadArray<PropertySyncFloatEntry>(ref reader);
    }

    static void WriteArray<T>(ref BinaryWriter writer, T[] values) where T : DataType {
        writer.WriteVarUInt((uint)values.Length);
        for (int i = 0; i < values.Length; i++) values[i].Write(ref writer);
    }

    static T[] ReadArray<T>(ref BinaryReader reader) where T : DataType, new() {
        int count = checked((int)reader.ReadVarUInt());
        T[] values = new T[count];
        for (int i = 0; i < count; i++) {
            values[i] = new T();
            values[i].Read(ref reader);
        }
        return values;
    }
}
