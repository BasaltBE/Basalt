using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public interface IInventoryTransactionData
{
    InventoryTransactionType Type { get; }
    void Read(BinaryReader reader);
    void Write(BinaryWriter writer);
}
