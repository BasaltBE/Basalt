using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeItemInstanceDescriptor : DataType
{
    public byte[]? RawData { get; set; }
    public int NetworkId { get; set; }
    public ushort StackSize { get; set; }
    public int Metadata { get; set; }
    public int NetworkBlockId { get; set; }
    public ItemInstanceUserData? ExtraData { get; set; }

    public void Read(BinaryReader reader)
    {
        NetworkId = reader.ReadZigZag();
        if (NetworkId == 0)
        {
            StackSize = 0;
            Metadata = 0;
            NetworkBlockId = 0;
            ExtraData = null;
            return;
        }

        StackSize = reader.ReadUInt16(true);
        Metadata = reader.ReadVarInt();
        NetworkBlockId = reader.ReadZigZag();

        int extrasLength = checked((int)reader.ReadVarInt());
        if (extrasLength <= 0)
        {
            ExtraData = null;
            return;
        }

        int extrasEndOffset = reader.Offset + extrasLength;
        ItemInstanceUserData extraData = new();
        extraData.Read(reader, NetworkId);
        ExtraData = extraData;
        if (reader.Offset < extrasEndOffset)
        {
            reader.Seek(extrasEndOffset);
        }
    }

    public void Write(BinaryWriter writer)
    {
        if (RawData is not null)
        {
            writer.WriteBytes(RawData);
            return;
        }

        writer.WriteZigZag(NetworkId);
        if (NetworkId == 0)
        {
            return;
        }

        writer.WriteUInt16(StackSize, true);
        writer.WriteVarInt(Metadata);
        writer.WriteZigZag(NetworkBlockId);

        if (ExtraData is null)
        {
            writer.WriteVarInt(0);
            return;
        }

        byte[] payloadBuffer = new byte[8192];
        int offset = 0;
        BinaryWriter payloadWriter = new(payloadBuffer, ref offset);
        ExtraData.Write(payloadWriter, NetworkId);
        ReadOnlySpan<byte> payload = payloadWriter.GetProcessedBytes();
        writer.WriteVarInt(payload.Length);
        writer.WriteBytes(payloadBuffer[payloadWriter.GetProcessedRange()]);
    }
}
