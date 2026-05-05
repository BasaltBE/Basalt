using Basalt.RakNet.Packets;
using Basalt.RakNet.Packets.Enums;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet
{
    public abstract class NetworkConnection
    {
        private const int DatagramWindowSize = 2048;
        private const int MaxOrderChannels = 32;
        private const int DatagramHeaderSize = 4;
        private const int AckResendMs = 300;

        private uint datagramWindowStart;
        private uint datagramWindowEnd = DatagramWindowSize;
        private uint highestDatagramSequence;
        private bool hasSeenDatagram;

        private uint reliableWindowStart;
        private uint reliableWindowEnd = DatagramWindowSize;
        private readonly HashSet<uint> reliableWindow = [];

        private readonly HashSet<uint> ackQueue = [];
        private readonly HashSet<uint> nackQueue = [];

        private readonly uint[] receiveOrderedIndex = new uint[MaxOrderChannels];
        private readonly uint[] receiveSequencedHighestIndex = new uint[MaxOrderChannels];
        private readonly Dictionary<int, Dictionary<uint, Frame>> receiveOrderedFrames = [];
        private readonly Dictionary<ushort, SplitReassembly> splitFrames = [];

        private uint sendSequence;
        private uint sendReliableIndex;
        private ushort splitId;
        private readonly uint[] sendOrderingIndex = new uint[MaxOrderChannels];
        private readonly uint[] sendSequencedIndex = new uint[MaxOrderChannels];
        private readonly LinkedList<QueuedFrame> outgoingFrames = [];
        private readonly Dictionary<uint, PendingDatagram> pendingDatagrams = [];

        protected virtual int MaxMtu => 1492;

        public virtual void HandleFrameSet(FrameSet frameSet)
        {
            uint sequence = frameSet.Sequence;
            if (IsOutOfDatagramWindow(sequence) || ackQueue.Contains(sequence))
            {
                return;
            }

            nackQueue.Remove(sequence);
            ackQueue.Add(sequence);

            if (!hasSeenDatagram || sequence > highestDatagramSequence)
            {
                highestDatagramSequence = sequence;
                hasSeenDatagram = true;
            }

            if (sequence == datagramWindowStart)
            {
                // Slide the receive window through contiguous received datagrams.
                while (ackQueue.Contains(datagramWindowStart))
                {
                    datagramWindowStart++;
                    datagramWindowEnd++;
                }
            }
            else if (sequence > datagramWindowStart)
            {
                // Gap detected: enqueue missing sequence numbers for NACK.
                for (uint i = datagramWindowStart; i < sequence; i++)
                {
                    if (!ackQueue.Contains(i))
                    {
                        nackQueue.Add(i);
                    }
                }
            }

            for (int i = 0; i < frameSet.Frames.Length; i++)
            {
                HandleIncomingFrame(frameSet.Frames[i]);
            }
        }

        public void HandleAck(Ack ack)
        {
            uint[] sequences = AckRecord.ExpandRecords(ack.Records);
            for (int i = 0; i < sequences.Length; i++)
            {
                pendingDatagrams.Remove(sequences[i]);
            }
        }

        public void HandleNack(Nack nack)
        {
            uint[] sequences = AckRecord.ExpandRecords(nack.Records);
            for (int i = 0; i < sequences.Length; i++)
            {
                if (!pendingDatagrams.Remove(sequences[i], out PendingDatagram pending))
                {
                    continue;
                }

                // Requeue original frames so they are repacked/sent on next tick.
                for (int j = pending.Frames.Length - 1; j >= 0; j--)
                {
                    outgoingFrames.AddFirst(new QueuedFrame(pending.Frames[j]));
                }
            }
        }

        public void Tick(long nowMs)
        {
            if (hasSeenDatagram)
            {
                uint diff = highestDatagramSequence - datagramWindowStart + 1;
                if (diff > 0)
                {
                    datagramWindowStart += diff;
                    datagramWindowEnd += diff;
                }
            }

            if (ackQueue.Count > 0)
            {
                SendControlPacket(Ack.FromSequences([.. ackQueue]));
                ackQueue.Clear();
            }

            if (nackQueue.Count > 0)
            {
                SendControlPacket(Nack.FromSequences([.. nackQueue]));
                nackQueue.Clear();
            }

            if (pendingDatagrams.Count > 0)
            {
                List<uint> expired = [];
                foreach ((uint sequence, PendingDatagram pending) in pendingDatagrams)
                {
                    // Fallback resend when ACK/NACK did not arrive in time.
                    if (nowMs - pending.SentAtMs >= AckResendMs)
                    {
                        expired.Add(sequence);
                    }
                }

                for (int i = 0; i < expired.Count; i++)
                {
                    uint sequence = expired[i];
                    if (!pendingDatagrams.Remove(sequence, out PendingDatagram pending))
                    {
                        continue;
                    }

                    for (int j = pending.Frames.Length - 1; j >= 0; j--)
                    {
                        outgoingFrames.AddFirst(new QueuedFrame(pending.Frames[j]));
                    }
                }
            }

            FlushOutgoing(nowMs);
        }

        protected virtual void HandleFrame(Frame frame)
        {
            // Should be overridden
        }

        protected abstract void SendMessage(ReadOnlySpan<byte> raw);

        protected void SendPayload(ReadOnlySpan<byte> payload, Reliability reliability = Reliability.ReliableOrdered, byte orderingChannel = 0)
        {
            if (NeedsOrdering(reliability) && orderingChannel >= MaxOrderChannels)
            {
                return;
            }

            int maxFramePayload = Math.Max(64, MaxMtu - 84);
            if (payload.Length <= maxFramePayload)
            {
                outgoingFrames.AddLast(new QueuedFrame(CreateOutgoingFrame(payload, reliability, orderingChannel, false, 0, 0)));
                return;
            }

            // Fragment payload into split frames when one frame would exceed MTU budget.
            int splitCount = (payload.Length + maxFramePayload - 1) / maxFramePayload;
            ushort currentSplitId = splitId++;
            uint orderingIndex = NeedsOrdering(reliability) ? sendOrderingIndex[orderingChannel]++ : 0;
            uint sequencedIndex = NeedsSequencedIndex(reliability) ? sendSequencedIndex[orderingChannel]++ : 0;

            int offset = 0;
            for (int splitIndex = 0; splitIndex < splitCount; splitIndex++)
            {
                int chunkLength = Math.Min(maxFramePayload, payload.Length - offset);
                ReadOnlySpan<byte> chunk = payload.Slice(offset, chunkLength);
                offset += chunkLength;

                uint reliableIndex = NeedsReliableIndex(reliability) ? sendReliableIndex++ : 0;
                Frame frame = new(
                    reliability: reliability,
                    isSplit: true,
                    bufferBitLength: (ushort)(chunk.Length * 8),
                    reliableIndex: reliableIndex,
                    sequencedIndex: sequencedIndex,
                    orderingIndex: orderingIndex,
                    orderingChannel: orderingChannel,
                    splitSize: (uint)splitCount,
                    splitId: currentSplitId,
                    splitIndex: (uint)splitIndex,
                    buffer: chunk.ToArray()
                );

                outgoingFrames.AddLast(new QueuedFrame(frame));
            }
        }

        private Frame CreateOutgoingFrame(ReadOnlySpan<byte> payload, Reliability reliability, byte orderingChannel, bool isSplit, uint splitSize, uint splitIndex)
        {
            return new(
                reliability: reliability,
                isSplit: isSplit,
                bufferBitLength: (ushort)(payload.Length * 8),
                reliableIndex: NeedsReliableIndex(reliability) ? sendReliableIndex++ : 0,
                sequencedIndex: NeedsSequencedIndex(reliability) ? sendSequencedIndex[orderingChannel]++ : 0,
                orderingIndex: NeedsOrdering(reliability) ? sendOrderingIndex[orderingChannel]++ : 0,
                orderingChannel: orderingChannel,
                splitSize: splitSize,
                splitId: splitId,
                splitIndex: splitIndex,
                buffer: payload.ToArray()
            );
        }

        private void FlushOutgoing(long nowMs)
        {
            if (outgoingFrames.Count == 0)
            {
                return;
            }

            int maxDatagram = Math.Max(256, MaxMtu - 36);
            byte[] datagramBuffer = new byte[Math.Max(2048, MaxMtu * 2)];
            List<Frame> packedFrames = [];
            int currentSize = DatagramHeaderSize;

            while (outgoingFrames.Count > 0)
            {
                Frame frame = outgoingFrames.First!.Value.Frame;
                int frameSize = Frame.Write(frame, datagramBuffer, 0);
                // Keep datagram under MTU envelope; spill remaining frames to next tick.
                if (packedFrames.Count > 0 && currentSize + frameSize > maxDatagram)
                {
                    break;
                }

                if (currentSize + frameSize > datagramBuffer.Length)
                {
                    break;
                }

                outgoingFrames.RemoveFirst();
                packedFrames.Add(frame);
                currentSize += frameSize;
            }

            if (packedFrames.Count == 0)
            {
                return;
            }

            uint sequence = sendSequence++;
            FrameSet frameSet = new(sequence, [.. packedFrames]);
            int length = FrameSet.Serialize(frameSet, datagramBuffer);
            SendMessage(datagramBuffer.AsSpan(0, length));

            bool hasReliable = false;
            for (int i = 0; i < packedFrames.Count; i++)
            {
                if (NeedsReliableIndex(packedFrames[i].Reliability))
                {
                    hasReliable = true;
                    break;
                }
            }

            if (hasReliable)
            {
                // Track reliable datagrams until ACK, NACK, or timeout-based resend.
                pendingDatagrams[sequence] = new PendingDatagram([.. packedFrames], nowMs);
            }
        }

        private void HandleIncomingFrame(Frame frame)
        {
            if (NeedsReliableIndex(frame.Reliability))
            {
                if (frame.ReliableIndex < reliableWindowStart || frame.ReliableIndex > reliableWindowEnd || reliableWindow.Contains(frame.ReliableIndex))
                {
                    return;
                }

                reliableWindow.Add(frame.ReliableIndex);
                if (frame.ReliableIndex == reliableWindowStart)
                {
                    while (reliableWindow.Remove(reliableWindowStart))
                    {
                        reliableWindowStart++;
                        reliableWindowEnd++;
                    }
                }
            }

            Frame? completeFrame = HandleSplit(frame);
            if (!completeFrame.HasValue)
            {
                return;
            }

            Frame incoming = completeFrame.Value;
            if (NeedsOrdering(incoming.Reliability))
            {
                int channel = incoming.OrderingChannel;
                if (channel < 0 || channel >= MaxOrderChannels)
                {
                    return;
                }
            }

            if (NeedsSequencedIndex(incoming.Reliability))
            {
                int channel = incoming.OrderingChannel;
                if (incoming.SequencedIndex < receiveSequencedHighestIndex[channel] || incoming.OrderingIndex < receiveOrderedIndex[channel])
                {
                    return;
                }

                receiveSequencedHighestIndex[channel] = incoming.SequencedIndex + 1;
                HandleFrame(incoming);
                return;
            }

            if (incoming.Reliability is Reliability.ReliableOrdered or Reliability.ReliableOrderedWithAckReceipt)
            {
                int channel = incoming.OrderingChannel;
                uint expected = receiveOrderedIndex[channel];
                if (incoming.OrderingIndex == expected)
                {
                    receiveSequencedHighestIndex[channel] = 0;
                    receiveOrderedIndex[channel] = incoming.OrderingIndex + 1;
                    HandleFrame(incoming);

                    if (receiveOrderedFrames.TryGetValue(channel, out Dictionary<uint, Frame>? channelQueue))
                    {
                        while (channelQueue.Remove(receiveOrderedIndex[channel], out Frame queued))
                        {
                            HandleFrame(queued);
                            receiveOrderedIndex[channel]++;
                        }

                        if (channelQueue.Count == 0)
                        {
                            receiveOrderedFrames.Remove(channel);
                        }
                    }
                }
                else if (incoming.OrderingIndex > expected)
                {
                    if (!receiveOrderedFrames.TryGetValue(channel, out Dictionary<uint, Frame>? channelQueue))
                    {
                        channelQueue = [];
                        receiveOrderedFrames[channel] = channelQueue;
                    }

                    if (channelQueue.Count < DatagramWindowSize)
                    {
                        channelQueue[incoming.OrderingIndex] = incoming;
                    }
                }

                return;
            }

            HandleFrame(incoming);
        }

        private Frame? HandleSplit(Frame frame)
        {
            if (!frame.IsSplit)
            {
                return frame;
            }

            uint totalParts = frame.SplitSize;
            if (totalParts == 0 || frame.SplitIndex >= totalParts)
            {
                return null;
            }

            if (!splitFrames.TryGetValue(frame.SplitId, out SplitReassembly? split))
            {
                split = new SplitReassembly(totalParts);
                splitFrames[frame.SplitId] = split;
            }
            else if (split.TotalParts != totalParts)
            {
                splitFrames.Remove(frame.SplitId);
                return null;
            }

            split.Add(frame.SplitIndex, frame);
            if (!split.IsComplete)
            {
                return null;
            }

            // All parts arrived: rebuild original payload and continue normal routing.
            int totalLength = split.GetTotalPayloadLength();
            byte[] payload = new byte[totalLength];
            int offset = 0;
            for (uint i = 0; i < split.TotalParts; i++)
            {
                Frame part = split.Parts[i]!.Value;
                part.Buffer.AsSpan().CopyTo(payload.AsSpan(offset));
                offset += part.Buffer.Length;
            }

            splitFrames.Remove(frame.SplitId);
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

        private void SendControlPacket(Ack packet)
        {
            byte[] buffer = new byte[2048];
            int length = Ack.Serialize(packet, buffer);
            SendMessage(buffer.AsSpan(0, length));
        }

        private void SendControlPacket(Nack packet)
        {
            byte[] buffer = new byte[2048];
            int length = Nack.Serialize(packet, buffer);
            SendMessage(buffer.AsSpan(0, length));
        }

        private bool IsOutOfDatagramWindow(uint sequence)
        {
            return sequence < datagramWindowStart || sequence > datagramWindowEnd;
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

        private readonly record struct QueuedFrame(Frame Frame);
        private readonly record struct PendingDatagram(Frame[] Frames, long SentAtMs);

        private sealed class SplitReassembly(uint totalParts)
        {
            public uint TotalParts { get; } = totalParts;
            public Frame?[] Parts { get; } = new Frame?[totalParts];
            private int receivedParts;

            public bool IsComplete => receivedParts == Parts.Length;

            public void Add(uint index, Frame frame)
            {
                if (Parts[index].HasValue)
                {
                    return;
                }

                Parts[index] = frame;
                receivedParts++;
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
}
