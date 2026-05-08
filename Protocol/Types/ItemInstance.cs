using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstance : DataType
{
    public ItemStack Stack { get; set; } = new();
    public int StackNetworkId { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Stack.NetworkId = reader.ReadZigZag();
        if (Stack.NetworkId == 0)
        {
            Stack.Count = 0;
            Stack.MetadataValue = 0;
            Stack.BlockRuntimeId = 0;
            Stack.ExtraData = [];
            StackNetworkId = 0;
            return;
        }

        Stack.Count = reader.ReadUInt16(true);
        Stack.MetadataValue = reader.ReadVarUInt();
        bool hasNetId = reader.ReadBool();
        StackNetworkId = hasNetId ? reader.ReadZigZag() : 0;
        Stack.BlockRuntimeId = reader.ReadZigZag();
        Stack.ExtraData = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(Stack.NetworkId);
        if (Stack.NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(Stack.Count, true);
        writer.WriteVarUInt(Stack.MetadataValue);
        bool hasNetId = StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteZigZag(StackNetworkId);
        }

        writer.WriteZigZag(Stack.BlockRuntimeId);
        byte[] extraData = Stack.ExtraData.Length == 0 ? [0, 0] : Stack.ExtraData;
        writer.WriteVarUInt((uint)extraData.Length);
        writer.WriteBytes(extraData);
    }
}
