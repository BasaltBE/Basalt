using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class InventoryMismatchData : InventoryTransactionVariant {
    public InventoryTransaction Actions = new();

    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }
}
