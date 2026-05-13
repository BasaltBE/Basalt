using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet;

public abstract class NetworkConnection
{
    private const int DatagramWindowSize = 2048;
    private const int MaxOrderChannels = 32;
    private const int DatagramHeaderSize = 4;
    private const int AckResendMs = 300;
    private const int ControlPacketBufferSize = 2048;

    private uint _datagramWindowStart;
    private uint _datagramWindowEnd = DatagramWindowSize;

    private uint _reliableWindowStart;
    private uint _reliableWindowEnd = DatagramWindowSize;

    private readonly HashSet<uint> _receivedDatagrams = [];
    private readonly HashSet<uint> _receivedReliableIndexes = [];

    private readonly HashSet<uint> _ackQueue = [];
    private readonly HashSet<uint> _nackQueue = [];

    private readonly uint[] _nextOrderedIndex = new uint[MaxOrderChannels];
    private readonly uint[] _highestSequencedIndex = new uint[MaxOrderChannels];

    private readonly Dictionary<int, Dictionary<uint, Frame>> _orderedFrameQueue = [];
    private readonly Dictionary<ushort, SplitReassembly> _splitFrames = [];

    private uint _sendSequence;
    private uint _sendReliableIndex;
    private ushort _splitId;

    private readonly uint[] _sendOrderingIndex = new uint[MaxOrderChannels];
    private readonly uint[] _sendSequencedIndex = new uint[MaxOrderChannels];

    private readonly LinkedList<Frame> _outgoingFrames = [];
    private readonly Dictionary<uint, PendingDatagram> _pendingDatagrams = [];
    private readonly object _sendLock = new();

    protected virtual int MaxMtu => 1492;

    public virtual void Disconnect()
    {
    }

    public virtual void HandleFrameSet(FrameSet frameSet)
    {
        uint sequence = frameSet.Sequence;

        if (sequence < _datagramWindowStart || sequence > _datagramWindowEnd || _receivedDatagrams.Contains(sequence))
        {
            return;
        }

        _receivedDatagrams.Add(sequence);
        lock (_sendLock)
        {
            _ackQueue.Add(sequence);
            _nackQueue.Remove(sequence);
        }

        if (sequence == _datagramWindowStart)
        {
            while (_receivedDatagrams.Remove(_datagramWindowStart))
            {
                _datagramWindowStart++;
                _datagramWindowEnd++;
            }
        }
        else
        {
            List<uint> missingDatagrams = [];
            for (uint missing = _datagramWindowStart; missing < sequence; missing++)
            {
                if (!_receivedDatagrams.Contains(missing))
                {
                    missingDatagrams.Add(missing);
                }
            }

            if (missingDatagrams.Count > 0)
            {
                lock (_sendLock)
                {
                    for (int i = 0; i < missingDatagrams.Count; i++)
                    {
                        _nackQueue.Add(missingDatagrams[i]);
                    }
                }
            }
        }

        foreach (Frame frame in frameSet.Frames)
        {
            HandleIncomingFrame(frame);
        }
    }

    public void HandleAck(Ack ack)
    {
        lock (_sendLock)
        {
            foreach (uint sequence in AckRecord.ExpandRecords(ack.Records))
            {
                _pendingDatagrams.Remove(sequence);
            }
        }
    }

    public void HandleNack(Nack nack)
    {
        lock (_sendLock)
        {
            foreach (uint sequence in AckRecord.ExpandRecords(nack.Records))
            {
                if (_pendingDatagrams.Remove(sequence, out PendingDatagram pending))
                {
                    RequeueFrames(pending.Frames);
                }
            }
        }
    }

    public void Tick(long nowMs)
    {
        lock (_sendLock)
        {
            if (_ackQueue.Count > 0)
            {
                byte[] buffer = new byte[ControlPacketBufferSize];
                int length = Ack.Serialize(Ack.FromSequences([.. _ackQueue]), buffer);
                SendMessage(buffer.AsSpan(0, length));
                _ackQueue.Clear();
            }

            if (_nackQueue.Count > 0)
            {
                byte[] buffer = new byte[ControlPacketBufferSize];
                int length = Nack.Serialize(Nack.FromSequences([.. _nackQueue]), buffer);
                SendMessage(buffer.AsSpan(0, length));
                _nackQueue.Clear();
            }

            foreach (uint sequence in _pendingDatagrams.Keys.ToArray())
            {
                if (!_pendingDatagrams.TryGetValue(sequence, out PendingDatagram pending))
                {
                    continue;
                }

                if (nowMs - pending.SentAtMs < AckResendMs)
                {
                    continue;
                }

                _pendingDatagrams.Remove(sequence);
                RequeueFrames(pending.Frames);
            }

            FlushOutgoing(nowMs);
        }
    }

    public void SendPacket(ReadOnlySpan<byte> payload, Reliability reliability = Reliability.ReliableOrdered)
    {
        SendPayload(payload, reliability);
    }

    protected void SendPayload(
        ReadOnlySpan<byte> payload,
        Reliability reliability = Reliability.ReliableOrdered,
        byte orderingChannel = 0)
    {
        if (NeedsOrdering(reliability) && orderingChannel >= MaxOrderChannels)
        {
            return;
        }

        int maxPayloadSize = Math.Max(64, MaxMtu - 84);

        lock (_sendLock)
        {
            if (payload.Length <= maxPayloadSize)
            {
                _outgoingFrames.AddLast(CreateFrame(payload, reliability, orderingChannel));
                return;
            }

            int splitCount = (payload.Length + maxPayloadSize - 1) / maxPayloadSize;
            ushort currentSplitId = _splitId++;

            uint orderingIndex = NeedsOrdering(reliability) ? _sendOrderingIndex[orderingChannel]++ : 0;
            uint sequencedIndex = NeedsSequencedIndex(reliability) ? _sendSequencedIndex[orderingChannel]++ : 0;

            int offset = 0;

            for (int splitIndex = 0; splitIndex < splitCount; splitIndex++)
            {
                int chunkLength = Math.Min(maxPayloadSize, payload.Length - offset);
                ReadOnlySpan<byte> chunk = payload.Slice(offset, chunkLength);
                offset += chunkLength;

                _outgoingFrames.AddLast(new Frame(
                    reliability: reliability,
                    isSplit: true,
                    bufferBitLength: (ushort)(chunk.Length * 8),
                    reliableIndex: NeedsReliableIndex(reliability) ? _sendReliableIndex++ : 0,
                    sequencedIndex: sequencedIndex,
                    orderingIndex: orderingIndex,
                    orderingChannel: orderingChannel,
                    splitSize: (uint)splitCount,
                    splitId: currentSplitId,
                    splitIndex: (uint)splitIndex,
                    buffer: chunk.ToArray()
                ));
            }
        }
    }

    protected virtual void HandleFrame(Frame frame)
    {
    }

    protected abstract void SendMessage(ReadOnlySpan<byte> raw);

    private void FlushOutgoing(long nowMs)
    {
        if (_outgoingFrames.Count == 0)
        {
            return;
        }

        int maxDatagramSize = Math.Max(256, MaxMtu - 36);
        byte[] datagramBuffer = new byte[Math.Max(ControlPacketBufferSize, MaxMtu * 2)];

        while (_outgoingFrames.Count > 0)
        {
            List<Frame> packedFrames = [];
            int currentSize = DatagramHeaderSize;

            while (_outgoingFrames.Count > 0)
            {
                Frame frame = _outgoingFrames.First!.Value;
                int frameSize = Frame.Write(frame, datagramBuffer, 0);

                if (packedFrames.Count > 0 && currentSize + frameSize > maxDatagramSize)
                {
                    break;
                }

                if (currentSize + frameSize > datagramBuffer.Length)
                {
                    break;
                }

                _outgoingFrames.RemoveFirst();
                packedFrames.Add(frame);
                currentSize += frameSize;
            }

            if (packedFrames.Count == 0)
            {
                break;
            }

            uint sequence = _sendSequence++;
            FrameSet frameSet = new(sequence, [.. packedFrames]);

            int length = FrameSet.Serialize(frameSet, datagramBuffer);
            SendMessage(datagramBuffer.AsSpan(0, length));

            if (packedFrames.Any(frame => NeedsReliableIndex(frame.Reliability)))
            {
                _pendingDatagrams[sequence] = new PendingDatagram([.. packedFrames], nowMs);
            }
        }
    }

    private void HandleIncomingFrame(Frame frame)
    {
        if (NeedsReliableIndex(frame.Reliability))
        {
            if (frame.ReliableIndex < _reliableWindowStart ||
                frame.ReliableIndex > _reliableWindowEnd ||
                _receivedReliableIndexes.Contains(frame.ReliableIndex))
            {
                return;
            }

            _receivedReliableIndexes.Add(frame.ReliableIndex);

            if (frame.ReliableIndex == _reliableWindowStart)
            {
                while (_receivedReliableIndexes.Remove(_reliableWindowStart))
                {
                    _reliableWindowStart++;
                    _reliableWindowEnd++;
                }
            }
        }

        Frame? completeFrame = ReassembleSplitFrame(frame);
        if (!completeFrame.HasValue)
        {
            return;
        }

        Frame incoming = completeFrame.Value;

        if (NeedsOrdering(incoming.Reliability) && incoming.OrderingChannel >= MaxOrderChannels)
        {
            return;
        }

        if (NeedsSequencedIndex(incoming.Reliability))
        {
            int channel = incoming.OrderingChannel;

            if (incoming.SequencedIndex < _highestSequencedIndex[channel] ||
                incoming.OrderingIndex < _nextOrderedIndex[channel])
            {
                return;
            }

            _highestSequencedIndex[channel] = incoming.SequencedIndex + 1;
            HandleFrame(incoming);
            return;
        }

        if (incoming.Reliability is Reliability.ReliableOrdered or Reliability.ReliableOrderedWithAckReceipt)
        {
            HandleOrderedFrame(incoming);
            return;
        }

        HandleFrame(incoming);
    }

    private void HandleOrderedFrame(Frame frame)
    {
        int channel = frame.OrderingChannel;
        uint expectedIndex = _nextOrderedIndex[channel];

        if (frame.OrderingIndex == expectedIndex)
        {
            _highestSequencedIndex[channel] = 0;
            _nextOrderedIndex[channel] = frame.OrderingIndex + 1;

            HandleFrame(frame);

            if (!_orderedFrameQueue.TryGetValue(channel, out Dictionary<uint, Frame>? queuedFrames))
            {
                return;
            }

            while (queuedFrames.Remove(_nextOrderedIndex[channel], out Frame queuedFrame))
            {
                HandleFrame(queuedFrame);
                _nextOrderedIndex[channel]++;
            }

            if (queuedFrames.Count == 0)
            {
                _orderedFrameQueue.Remove(channel);
            }

            return;
        }

        if (frame.OrderingIndex <= expectedIndex)
        {
            return;
        }

        if (!_orderedFrameQueue.TryGetValue(channel, out Dictionary<uint, Frame>? channelQueue))
        {
            channelQueue = [];
            _orderedFrameQueue[channel] = channelQueue;
        }

        if (channelQueue.Count < DatagramWindowSize)
        {
            channelQueue[frame.OrderingIndex] = frame;
        }
    }

    private Frame? ReassembleSplitFrame(Frame frame)
    {
        if (!frame.IsSplit)
        {
            return frame;
        }

        if (frame.SplitSize == 0 ||
            frame.SplitSize > DatagramWindowSize ||
            frame.SplitIndex >= frame.SplitSize)
        {
            return null;
        }

        if (!_splitFrames.TryGetValue(frame.SplitId, out SplitReassembly? split))
        {
            split = new SplitReassembly(frame.SplitSize);
            _splitFrames[frame.SplitId] = split;
        }
        else if (split.TotalParts != frame.SplitSize)
        {
            _splitFrames.Remove(frame.SplitId);
            return null;
        }

        split.Add(frame.SplitIndex, frame);

        if (!split.IsComplete)
        {
            return null;
        }

        int totalLength = split.GetTotalPayloadLength();
        byte[] payload = new byte[totalLength];

        int offset = 0;

        for (uint i = 0; i < split.TotalParts; i++)
        {
            Frame part = split.Parts[i]!.Value;
            part.Buffer.AsSpan().CopyTo(payload.AsSpan(offset));
            offset += part.Buffer.Length;
        }

        _splitFrames.Remove(frame.SplitId);

        return new Frame(
            reliability: frame.Reliability,
            isSplit: false,
            bufferBitLength: (ushort)(payload.Length * 8),
            reliableIndex: frame.ReliableIndex,
            sequencedIndex: frame.SequencedIndex,
            orderingIndex: frame.OrderingIndex,
            orderingChannel: frame.OrderingChannel,
            buffer: payload
        );
    }

    private Frame CreateFrame(ReadOnlySpan<byte> payload, Reliability reliability, byte orderingChannel)
    {
        return new Frame(
            reliability: reliability,
            isSplit: false,
            bufferBitLength: (ushort)(payload.Length * 8),
            reliableIndex: NeedsReliableIndex(reliability) ? _sendReliableIndex++ : 0,
            sequencedIndex: NeedsSequencedIndex(reliability) ? _sendSequencedIndex[orderingChannel]++ : 0,
            orderingIndex: NeedsOrdering(reliability) ? _sendOrderingIndex[orderingChannel]++ : 0,
            orderingChannel: orderingChannel,
            buffer: payload.ToArray()
        );
    }

    private void RequeueFrames(Frame[] frames)
    {
        for (int i = frames.Length - 1; i >= 0; i--)
        {
            _outgoingFrames.AddFirst(frames[i]);
        }
    }

    private static bool NeedsReliableIndex(Reliability reliability)
    {
        return reliability is
            Reliability.Reliable or
            Reliability.ReliableOrdered or
            Reliability.ReliableSequenced or
            Reliability.ReliableWithAckReceipt or
            Reliability.ReliableOrderedWithAckReceipt;
    }

    private static bool NeedsSequencedIndex(Reliability reliability)
    {
        return reliability is
            Reliability.UnreliableSequenced or
            Reliability.ReliableSequenced;
    }

    private static bool NeedsOrdering(Reliability reliability)
    {
        return reliability is
            Reliability.UnreliableSequenced or
            Reliability.ReliableOrdered or
            Reliability.ReliableSequenced or
            Reliability.ReliableOrderedWithAckReceipt;
    }

    private readonly record struct PendingDatagram(Frame[] Frames, long SentAtMs);

    private sealed class SplitReassembly
    {
        public uint TotalParts { get; }
        public Frame?[] Parts { get; }

        private int _receivedParts;

        public bool IsComplete => _receivedParts == Parts.Length;

        public SplitReassembly(uint totalParts)
        {
            TotalParts = totalParts;
            Parts = new Frame?[totalParts];
        }

        public void Add(uint index, Frame frame)
        {
            if (index >= Parts.Length || Parts[index].HasValue)
            {
                return;
            }

            Parts[index] = frame;
            _receivedParts++;
        }

        public int GetTotalPayloadLength()
        {
            int total = 0;

            for (int i = 0; i < Parts.Length; i++)
            {
                total += Parts[i]!.Value.Buffer.Length;
            }

            return total;
        }
    }
}
