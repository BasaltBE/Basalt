using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct Ack(AckRecord[]? records = null)
{
    public const byte PacketId = 0xc0;

    public AckRecord[] Records = records ?? [];

    public static Ack Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 3)
        {
            throw new InvalidOperationException("Invalid Ack length.");
        }

        int offset = 0;
        byte packetId = src.ReadUInt8(offset);
        offset += 1;

        if (packetId != PacketId)
        {
            throw new InvalidOperationException("Invalid packet id.");
        }

        ushort count = src.ReadUInt16(offset, false);
        offset += 2;

        AckRecord[] records = new AckRecord[count];
        int recordsRead = 0;
        for (int i = 0; i < count && offset < src.Length; i++)
        {
            int remaining = src.Length - offset;
            if (remaining < 4)
            {
                // Smallest possible record is: type + single triad.
                break;
            }

            bool isSingle = src.ReadUInt8(offset) != 0;
            if (!isSingle && remaining < 7)
            {
                // Range record needs: type + start triad + end triad.
                break;
            }

            AckRecord record = AckRecord.Read(src, out int bytesRead, offset);
            offset += bytesRead;
            records[recordsRead++] = record;
        }

        if (recordsRead != records.Length)
        {
            Array.Resize(ref records, recordsRead);
        }

        return new(records);
    }

    public static int Serialize(Ack packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        dest.WriteUInt16((ushort)packet.Records.Length, offset, false);
        offset += 2;

        for (int i = 0; i < packet.Records.Length; i++)
        {
            offset += AckRecord.Write(packet.Records[i], dest, offset);
        }

        return offset;
    }

    public static Ack FromSequences(Span<uint> sequences)
    {
        return new(AckRecord.PackSequences(sequences));
    }
}
