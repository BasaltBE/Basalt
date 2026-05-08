using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstance : DataType
{
    public NetworkItemStackDescriptor Stack { get; set; } = new();
    public int StackNetworkId { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Stack.NetworkId = reader.ReadZigZag();
        if (Stack.NetworkId == 0)
        {
            Stack.StackSize = 0;
            Stack.Metadata = 0;
            Stack.NetworkBlockId = 0;
            Stack.ExtraData = [];
            StackNetworkId = 0;
            return;
        }

        Stack.StackSize = reader.ReadUInt16(true);
        Stack.Metadata = reader.ReadVarUInt();
        bool hasNetId = reader.ReadBool();
        StackNetworkId = hasNetId ? reader.ReadZigZag() : 0;
        Stack.NetworkBlockId = reader.ReadZigZag();
        Stack.ExtraData = reader.ReadBytes(checked((int)reader.ReadVarUInt())).ToArray();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(Stack.NetworkId);
        if (Stack.NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(Stack.StackSize, true);
        writer.WriteVarUInt(Stack.Metadata);
        bool hasNetId = StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteZigZag(StackNetworkId);
        }

        writer.WriteZigZag(Stack.NetworkBlockId);
        byte[] extraData = Stack.ExtraData.Length == 0 ? [0, 0] : Stack.ExtraData;
        writer.WriteVarUInt((uint)extraData.Length);
        writer.WriteBytes(extraData);
    }
}
