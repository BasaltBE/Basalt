using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct FrameSet(uint sequence = 0, Frame[]? frames = null)
{
    public const byte PacketId = 0x80;
    public uint Sequence = sequence;
    public Frame[] Frames = frames ?? [];

    public static FrameSet Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 4)
        {
            throw new InvalidOperationException("Invalid FrameSet length.");
        }

        byte packetId = src.ReadUInt8(0);
        if (packetId < 0x80 || packetId > 0x8d)
        {
            throw new InvalidOperationException("Invalid FrameSet packet id.");
        }

        int offset = 1;
        uint sequence = src.ReadUInt24(offset, true);
        offset += 3;

        List<Frame> frames = [];
        while (offset < src.Length)
        {
            Frame frame = Frame.Read(src, out int bytesRead, offset);
            if (bytesRead <= 0)
            {
                throw new InvalidOperationException("Invalid Frame length.");
            }

            offset += bytesRead;
            frames.Add(frame);
        }

        FrameSet frameSet = new(sequence, frames.ToArray());
        return frameSet;
    }

    public static int Serialize(FrameSet frameSet, Span<byte> dest)
    {
        return Serialize(frameSet.Sequence, frameSet.Frames, dest);
    }

    public static int Serialize(uint sequence, IReadOnlyList<Frame> frames, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(FrameSet.PacketId, offset);
        offset += 1;

        dest.WriteUInt24(sequence, offset, true);
        offset += 3;

        for (int i = 0; i < frames.Count; i++)
        {
            offset += Frame.Write(frames[i], dest, offset);
        }

        return offset;
    }
}
