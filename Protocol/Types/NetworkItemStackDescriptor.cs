using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class NetworkItemStackDescriptor : DataType
{
    public int NetworkId { get; set; } = 0;
    public ushort StackSize { get; set; } = 0;
    public int Metadata { get; set; } = 0;
    public int? ItemStackId { get; set; }
    public int NetworkBlockId { get; set; } = 0;
    public ItemInstanceUserData? ExtraData { get; set; }

    public void Read(ref BinaryReader reader)
    {
        NetworkId = reader.ReadZigZag();
        if (NetworkId == 0)
        {
            StackSize = 0;
            Metadata = 0;
            ItemStackId = null;
            NetworkBlockId = 0;
            ExtraData = null;
            return;
        }

        StackSize = reader.ReadUInt16(true);
        Metadata = reader.ReadVarInt();

        bool hasStackId = reader.ReadBool();
        ItemStackId = hasStackId ? reader.ReadZigZag() : null;

        NetworkBlockId = reader.ReadZigZag();

        int extrasLength = reader.ReadVarInt();
        if (extrasLength < 0)
        {
            throw new FormatException("Negative extras length in network item stack descriptor.");
        }
        if (extrasLength == 0)
        {
            ExtraData = null;
            return;
        }

        int extrasEndOffset = reader.Offset + extrasLength;
        ItemInstanceUserData extraData = new();
        extraData.Read(ref reader, NetworkId);
        ExtraData = extraData;
        if (reader.Offset < extrasEndOffset)
        {
            reader.Seek(extrasEndOffset);
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(NetworkId);
        if (NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(StackSize, true);
        writer.WriteVarInt(Metadata);

        bool hasStackId = ItemStackId.HasValue && ItemStackId.Value != 0;
        writer.WriteBool(hasStackId);
        if (hasStackId)
        {
            writer.WriteZigZag(ItemStackId!.Value);
        }

        writer.WriteZigZag(NetworkBlockId);
        if (ExtraData is null)
        {
            writer.WriteVarInt(0);
            return;
        }

        byte[] payloadBuffer = new byte[8192];
        BinaryWriter payloadWriter = new(payloadBuffer);
        ExtraData.Write(ref payloadWriter, NetworkId);
        ReadOnlySpan<byte> payload = payloadWriter.GetBuffer();
        writer.WriteVarInt(payload.Length);
        writer.WriteBytes(payload);
    }
}
