using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StoreEntryPointInfo
{
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;

    public void Read(ref BinaryReader reader)
    {
        StoreId = reader.ReadVarString();
        StoreName = reader.ReadVarString();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(StoreId);
        writer.WriteVarString(StoreName);
    }
}
