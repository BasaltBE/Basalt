using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PropertySyncData : DataType {
    public PropertySyncIntEntry[] IntEntries = [];
    public PropertySyncFloatEntry[] FloatEntries = [];

    public override void Read(ref BinaryReader reader) {
        IntEntries = new PropertySyncIntEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < IntEntries.Length; i++) {
            PropertySyncIntEntry entry = new();
            entry.Read(ref reader);
            IntEntries[i] = entry;
        }

        FloatEntries = new PropertySyncFloatEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < FloatEntries.Length; i++) {
            PropertySyncFloatEntry entry = new();
            entry.Read(ref reader);
            FloatEntries[i] = entry;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)IntEntries.Length);
        foreach (PropertySyncIntEntry entry in IntEntries)
            entry.Write(ref writer);

        writer.WriteVarUInt((uint)FloatEntries.Length);
        foreach (PropertySyncFloatEntry entry in FloatEntries)
            entry.Write(ref writer);
    }
}
