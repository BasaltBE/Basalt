using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemInstance : DataType
{
    /// <summary>
    /// Item stack payload.
    /// </summary>
    public LegacyItem Stack = new();
    /// <summary>
    /// Stack network id value.
    /// </summary>
    public int StackNetworkId;
    public void Read(BinaryReader reader)
    {
        Stack.NetworkId = reader.ReadZigZag();
        if (Stack.NetworkId == 0)
        {
            Stack.StackSize = 0;
            Stack.Metadata = 0;
            Stack.NetworkBlockId = 0;
            Stack.ExtraData = null;
            StackNetworkId = 0;
            return;
        }

        Stack.StackSize = reader.ReadUInt16(true);
        Stack.Metadata = reader.ReadVarInt();
        bool hasNetId = reader.ReadBool();
        StackNetworkId = hasNetId ? reader.ReadZigZag() : 0;
        Stack.NetworkBlockId = reader.ReadZigZag();

        int extrasLength = checked((int)reader.ReadVarUInt());
        if (extrasLength > reader.Remaining)
        {
            throw new FormatException("Invalid extras length in item instance.");
        }
        if (extrasLength == 0)
        {
            Stack.ExtraData = null;
            return;
        }

        int extrasEndOffset = reader.Offset + extrasLength;
        ItemInstanceUserData extraData = new();
        extraData.Read(reader, Stack.NetworkId);
        Stack.ExtraData = extraData;
        if (reader.Offset < extrasEndOffset)
        {
            reader.Seek(extrasEndOffset);
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(Stack.NetworkId);
        if (Stack.NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(Stack.StackSize, true);
        writer.WriteVarInt(Stack.Metadata);
        bool hasNetId = StackNetworkId != 0;
        writer.WriteBool(hasNetId);
        if (hasNetId)
        {
            writer.WriteZigZag(StackNetworkId);
        }

        writer.WriteZigZag(Stack.NetworkBlockId);
        if (Stack.ExtraData is null)
        {
            writer.WriteVarUInt(0);
            return;
        }

        byte[] payloadBuffer = new byte[8192];
        int offset = 0;
        BinaryWriter payloadWriter = new(payloadBuffer, ref offset);
        Stack.ExtraData.Write(payloadWriter, Stack.NetworkId);
        ReadOnlySpan<byte> payload = payloadWriter.GetProcessedBytes();
        writer.WriteVarUInt((uint)payload.Length);
        writer.WriteBytes(payloadBuffer[payloadWriter.GetProcessedRange()]);
    }
}
