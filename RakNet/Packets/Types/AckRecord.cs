using Basalt.Binary;

namespace Basalt.RakNet.Packets.Types;

public struct AckRecord(uint start = 0, uint end = 0, bool isSingle = true)
{
    public bool IsSingle = isSingle;
    public uint Start = start;
    public uint End = end == 0 ? start : end;

    public static AckRecord Read(ReadOnlySpan<byte> src, out int bytesRead, int offset = 0)
    {
        byte isSingleFlag = src.ReadUInt8(offset);
        offset += 1;

        if (isSingleFlag != 0)
        {
            uint value = src.ReadUInt24(offset, true);
            bytesRead = 4;
            return new(value, value, true);
        }

        uint start = src.ReadUInt24(offset, true);
        offset += 3;
        uint end = src.ReadUInt24(offset, true);
        bytesRead = 7;
        return new(start, end, false);
    }

    public static int Write(AckRecord record, Span<byte> dest, int offset = 0)
    {
        int startOffset = offset;
        dest.WriteUInt8(record.IsSingle ? (byte)1 : (byte)0, offset);
        offset += 1;

        dest.WriteUInt24(record.Start, offset, true);
        offset += 3;

        if (!record.IsSingle)
        {
            dest.WriteUInt24(record.End, offset, true);
            offset += 3;
        }

        return offset - startOffset;
    }

    public static AckRecord[] PackSequences(uint[] sequences)
    {
        if (sequences.Length == 0)
        {
            return [];
        }

        // Sort so we can collapse consecutive values into range records.
        uint[] sorted = sequences.ToArray();
        Array.Sort(sorted);

        List<AckRecord> records = [];
        uint start = sorted[0];
        uint last = sorted[0];

        for (int i = 1; i < sorted.Length; i++)
        {
            uint current = sorted[i];
            if (current == last)
            {
                // Skip duplicates.
                continue;
            }

            if (current == last + 1)
            {
                last = current;
                continue;
            }

            records.Add(start == last ? new AckRecord(start, start, true) : new AckRecord(start, last, false));
            start = current;
            last = current;
        }

        records.Add(start == last ? new AckRecord(start, start, true) : new AckRecord(start, last, false));
        return records.ToArray();
    }

    public static uint[] ExpandRecords(AckRecord[] records, int maxTotal = 4096, uint maxRangeLength = 512)
    {
        List<uint> sequences = [];
        for (int i = 0; i < records.Length; i++)
        {
            if (sequences.Count >= maxTotal)
            {
                break;
            }

            AckRecord record = records[i];
            if (record.IsSingle)
            {
                sequences.Add(record.Start);
                continue;
            }

            uint end = record.End;
            if (end > record.Start && end - record.Start > maxRangeLength)
            {
                // Clamp huge ranges to avoid pathological payload expansion.
                end = record.Start + maxRangeLength;
            }

            for (uint value = record.Start; value <= end; value++)
            {
                sequences.Add(value);
                if (sequences.Count >= maxTotal)
                {
                    break;
                }
            }
        }

        return sequences.ToArray();
    }
}
