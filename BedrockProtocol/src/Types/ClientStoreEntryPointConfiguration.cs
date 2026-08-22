using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ClientStoreEntryPointConfiguration : DataType {
    public string StoreId = string.Empty;
    public string StoreName = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(StoreId);
        writer.WriteVarString(StoreName);
    }

    public override void Read(ref BinaryReader reader) {
        StoreId = reader.ReadVarString();
        StoreName = reader.ReadVarString();
    }
}
