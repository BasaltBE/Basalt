using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ClientStoreEntryPointConfiguration : DataType {
    public string StoreId = string.Empty;
    public string StoreName = string.Empty;

    public override void Read(ref BinaryReader reader) {
        StoreId = reader.ReadVarString();
        StoreName = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(StoreId);
        writer.WriteVarString(StoreName);
    }
}
