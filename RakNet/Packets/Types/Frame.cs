using Basalt.Binary;
using Basalt.RakNet.Packets.Enums;

namespace Basalt.RakNet.Packets.Types;

public struct Frame(
    Reliability reliability = Reliability.Unreliable,
    bool isSplit = false,
    ushort bufferBitLength = 0,
    uint reliableIndex = 0,
    uint sequencedIndex = 0,
    uint orderingIndex = 0,
    byte orderingChannel = 0,
    uint splitSize = 0,
    ushort splitId = 0,
    uint splitIndex = 0,
    ReadOnlyMemory<byte> buffer = default
)
{
    public Reliability Reliability = reliability;
    public bool IsSplit = isSplit;
    public ushort BufferBitLength = bufferBitLength;
    public uint ReliableIndex = reliableIndex;
    public uint SequencedIndex = sequencedIndex;
    public uint OrderingIndex = orderingIndex;
    public byte OrderingChannel = orderingChannel;
    public uint SplitSize = splitSize;
    public ushort SplitId = splitId;
    public uint SplitIndex = splitIndex;
    public ReadOnlyMemory<byte> Buffer = buffer;

    public static Frame Read(ReadOnlySpan<byte> src, out int bytesRead, int offset = 0)
    {
        int startOffset = offset;

        byte flags = src.ReadUInt8(offset);
        offset += 1;

        Reliability reliability = (Reliability)((flags >> 5) & 0x07);
        bool isSplit = (flags & 0x10) != 0;

        ushort bufferBitLength = src.ReadUInt16(offset, false);
        offset += 2;

        uint reliableIndex = 0;
        if (NeedsReliableIndex(reliability))
        {
            reliableIndex = src.ReadUInt24(offset, true);
            offset += 3;
        }

        uint sequencedIndex = 0;
        if (NeedsSequencedIndex(reliability))
        {
            sequencedIndex = src.ReadUInt24(offset, true);
            offset += 3;
        }

        uint orderingIndex = 0;
        byte orderingChannel = 0;
        if (NeedsOrdering(reliability))
        {
            orderingIndex = src.ReadUInt24(offset, true);
            offset += 3;
            orderingChannel = src.ReadUInt8(offset);
            offset += 1;
        }

        uint splitSize = 0;
        ushort splitId = 0;
        uint splitIndex = 0;
        if (isSplit)
        {
            splitSize = src.ReadUInt32(offset, false);
            offset += 4;
            splitId = src.ReadUInt16(offset, false);
            offset += 2;
            splitIndex = src.ReadUInt32(offset, false);
            offset += 4;
        }

        int bufferByteLength = (bufferBitLength + 7) / 8;
        ReadOnlyMemory<byte> buffer = src.Slice(offset, bufferByteLength).ToArray();
        offset += bufferByteLength;

        bytesRead = offset - startOffset;
        return new(
            reliability,
            isSplit,
            bufferBitLength,
            reliableIndex,
            sequencedIndex,
            orderingIndex,
            orderingChannel,
            splitSize,
            splitId,
            splitIndex,
            buffer
        );
    }

    public static int Write(Frame frame, Span<byte> dest, int offset = 0)
    {
        int startOffset = offset;

        byte flags = (byte)(((byte)frame.Reliability & 0x07) << 5);
        if (frame.IsSplit)
        {
            flags |= 0x10;
        }

        dest.WriteUInt8(flags, offset);
        offset += 1;

        ushort bufferBitLength = frame.BufferBitLength != 0 ? frame.BufferBitLength : (ushort)(frame.Buffer.Length * 8);
        dest.WriteUInt16(bufferBitLength, offset, false);
        offset += 2;

        if (NeedsReliableIndex(frame.Reliability))
        {
            dest.WriteUInt24(frame.ReliableIndex, offset, true);
            offset += 3;
        }

        if (NeedsSequencedIndex(frame.Reliability))
        {
            dest.WriteUInt24(frame.SequencedIndex, offset, true);
            offset += 3;
        }

        if (NeedsOrdering(frame.Reliability))
        {
            dest.WriteUInt24(frame.OrderingIndex, offset, true);
            offset += 3;
            dest.WriteUInt8(frame.OrderingChannel, offset);
            offset += 1;
        }

        if (frame.IsSplit)
        {
            dest.WriteUInt32(frame.SplitSize, offset, false);
            offset += 4;
            dest.WriteUInt16(frame.SplitId, offset, false);
            offset += 2;
            dest.WriteUInt32(frame.SplitIndex, offset, false);
            offset += 4;
        }

        frame.Buffer.Span.CopyTo(dest[offset..]);
        offset += frame.Buffer.Length;

        return offset - startOffset;
    }

    public static int GetSize(Frame frame)
    {
        int size = 3 + frame.Buffer.Length;

        if (NeedsReliableIndex(frame.Reliability))
        {
            size += 3;
        }

        if (NeedsSequencedIndex(frame.Reliability))
        {
            size += 3;
        }

        if (NeedsOrdering(frame.Reliability))
        {
            size += 4;
        }

        if (frame.IsSplit)
        {
            size += 10;
        }

        return size;
    }

    private static bool NeedsReliableIndex(Reliability reliability)
    {
        return reliability is Reliability.Reliable
            or Reliability.ReliableOrdered
            or Reliability.ReliableSequenced
            or Reliability.ReliableWithAckReceipt
            or Reliability.ReliableOrderedWithAckReceipt;
    }

    private static bool NeedsSequencedIndex(Reliability reliability)
    {
        return reliability is Reliability.UnreliableSequenced or Reliability.ReliableSequenced;
    }

    private static bool NeedsOrdering(Reliability reliability)
    {
        return reliability is Reliability.UnreliableSequenced
            or Reliability.ReliableOrdered
            or Reliability.ReliableSequenced
            or Reliability.ReliableOrderedWithAckReceipt;
    }
}
