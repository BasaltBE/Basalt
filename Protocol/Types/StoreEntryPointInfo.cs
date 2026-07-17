using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class StoreEntryPointInfo : DataType
{
    /// <summary>
    /// The id of the store
    /// </summary>
    public string StoreId = string.Empty;

    /// <summary>
    /// The name of the store
    /// </summary>
    public string StoreName = string.Empty;

    public void Read(BinaryReader reader)
    {
        StoreId = reader.ReadVarString();
        StoreName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(StoreId);
        writer.WriteVarString(StoreName);
    }
}


