using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public sealed class DataStoreChange : DataStoreChangeInfo {
    public override DataStoreChangeAction Action => DataStoreChangeAction.Change;
    public override string DataStoreName { get; set; } = string.Empty;
    public string Property = string.Empty;
    public uint UpdateCount;
    public DataStorePropertyValue Value = DataStorePropertyValue.None();

    public override void Read(Binary.BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        UpdateCount = reader.ReadUInt32(true);
        Value = new DataStorePropertyValue();
        Value.Read(reader);
    }

    public override void Write(Binary.BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteUInt32(UpdateCount, true);
        Value.Write(writer);
    }
}
