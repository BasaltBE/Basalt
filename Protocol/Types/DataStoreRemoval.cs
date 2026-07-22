using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public sealed class DataStoreRemoval : DataStoreChangeInfo {
    public override DataStoreChangeAction Action => DataStoreChangeAction.Removal;
    public override string DataStoreName { get; set; } = string.Empty;

    public override void Read(Binary.BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
    }

    public override void Write(Binary.BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
    }
}
