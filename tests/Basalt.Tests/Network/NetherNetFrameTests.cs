namespace Basalt.Tests;

using Basalt.Core.Nethernet;

public sealed class NetherNetFrameTests {
    [Fact]
    public void FragmentedPayloadReassemblesInOrder() {
        byte[] expected = new byte[257];
        Random.Shared.NextBytes(expected);
        List<byte[]> frames = [];

        NetherNetFrame.Send(expected, 31, (frame, length) => frames.Add(frame[..length]));

        using NetherNetReassembler reassembler = new();
        byte[] actual = Array.Empty<byte>();
        for (int i = 0; i < frames.Count; i++) {
            bool complete = reassembler.Add(frames[i], out actual);
            Assert.Equal(i == frames.Count - 1, complete);
        }

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EmptyPayloadProducesOneMessage() {
        byte[]? frame = null;
        NetherNetFrame.Send([], 32, (value, length) => frame = value[..length]);

        using NetherNetReassembler reassembler = new();
        Assert.True(reassembler.Add(frame!, out byte[] payload));
        Assert.Empty(payload);
    }

    [Fact]
    public void UnexpectedFragmentSequenceIsRejected() {
        using NetherNetReassembler reassembler = new();

        Assert.False(reassembler.Add([2, 1], out _));
        Assert.False(reassembler.Add([0, 2], out _));
        Assert.False(reassembler.Add([1, 3], out _));
    }
}
