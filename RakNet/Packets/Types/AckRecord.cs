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

    /// <summary>
    /// Packs sorted sequences into ACK records. The input span is sorted in-place.
    /// </summary>
    public static AckRecord[] PackSequences(Span<uint> sequences)
    {
        if (sequences.Length == 0)
        {
            return [];
        }

        sequences.Sort();

        List<AckRecord> records = [];
        uint start = sequences[0];
        uint last = sequences[0];

        for (int i = 1; i < sequences.Length; i++)
        {
            uint current = sequences[i];
            if (current == last)
            {
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
        return [.. records];
    }

    /// <summary>
    /// Iterates all sequence numbers in the given records without allocating.
    /// This is called very often so the less allocations it can do is bettre
    /// </summary>
    public static ExpandedRecordEnumerator EnumerateRecords(AckRecord[] records)
    {
        return new ExpandedRecordEnumerator(records);
    }

    public ref struct ExpandedRecordEnumerator
    {
        private readonly AckRecord[] _records;
        private int _recordIndex;
        private uint _current;
        private uint _rangeEnd;
        private bool _inRange;
        private bool _started;

        public ExpandedRecordEnumerator(AckRecord[] records)
        {
            _records = records;
            _recordIndex = 0;
            _current = 0;
            _rangeEnd = 0;
            _inRange = false;
            _started = false;
        }

        public uint Current => _current;

        public ExpandedRecordEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_inRange)
            {
                if (_current < _rangeEnd)
                {
                    _current++;
                    return true;
                }

                _inRange = false;
                _recordIndex++;
            }
            else if (_started)
            {
                _recordIndex++;
            }

            _started = true;

            while (_recordIndex < _records.Length)
            {
                AckRecord record = _records[_recordIndex];

                if (record.IsSingle)
                {
                    _current = record.Start;
                    return true;
                }

                if (record.End < record.Start)
                {
                    _recordIndex++;
                    continue;
                }

                _current = record.Start;
                _rangeEnd = record.End;
                _inRange = _current < _rangeEnd;
                return true;
            }

            return false;
        }
    }
}
