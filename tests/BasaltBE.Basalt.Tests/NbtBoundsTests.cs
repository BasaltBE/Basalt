using BasaltBE.Nbt.Types;
using NbtCodec = BasaltBE.Nbt.Nbt;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class NbtBoundsTests {
    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(12)]
    public void TruncatedCollectionsRejectLengthsBeforeAllocation(byte type) {
        byte[] data = type == 9
            ? [type, 0, 0, 1, 255, 255, 255, 0]
            : [type, 0, 0, 255, 255, 255, 0];

        Assert.Throws<InvalidDataException>(() => {
            int offset = 0;
            NbtCodec.Read(data, ref offset, Options.LittleEndian);
        });
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(12)]
    public void TruncatedVarIntCollectionsRejectLengthsBeforeAllocation(byte type) {
        byte[] data = type == 9
            ? [type, 0, 1, 254, 255, 255, 15]
            : [type, 0, 254, 255, 255, 15];
        Options options = new() { VarInt = true, StringLengthVarInt = true };

        Assert.Throws<InvalidDataException>(() => {
            int offset = 0;
            NbtCodec.Read(data, ref offset, options);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollectionsRoundTripInBothFormats(bool varInt) {
        Options options = varInt ? new Options { VarInt = true, StringLengthVarInt = true } : Options.LittleEndian;
        CompoundTag root = new();
        root.Add("list", new ListTag { Value = [new ByteTag { Value = 1 }, new ByteTag { Value = 2 }] });
        root.Add("ints", new IntArrayTag { Value = [int.MinValue, 0, int.MaxValue] });
        root.Add("longs", new LongArrayTag { Value = [long.MinValue, 0, long.MaxValue] });
        byte[] data = new byte[root.GetSize(options)];
        int offset = 0;
        NbtCodec.Write(root, data, ref offset, options);
        offset = 0;
        CompoundTag result = Assert.IsType<CompoundTag>(NbtCodec.Read(data, ref offset, options));

        Assert.Equal(2, result.Get<ListTag>("list")!.Value.Count);
        Assert.Equal(new[] { int.MinValue, 0, int.MaxValue }, result.Get<IntArrayTag>("ints")!.Value);
        Assert.Equal(new[] { long.MinValue, 0, long.MaxValue }, result.Get<LongArrayTag>("longs")!.Value);
        Assert.Equal(data.Length, offset);
    }

    [Fact]
    public void BoundedReadRejectsExcessiveNestingAndTagCounts() {
        CompoundTag root = new();
        root.Add("first", new CompoundTag().Add("second", new ByteTag { Value = 1 }));
        byte[] data = new byte[root.GetSize()];
        int offset = 0;
        NbtCodec.Write(root, data, ref offset);

        Assert.Throws<InvalidDataException>(() => {
            int readOffset = 0;
            NbtCodec.Read(data, ref readOffset, Options.LittleEndian, new NbtReadLimits(2, 10, 10, 10));
        });
        Assert.Throws<InvalidDataException>(() => {
            int readOffset = 0;
            NbtCodec.Read(data, ref readOffset, Options.LittleEndian, new NbtReadLimits(10, 2, 10, 10));
        });
    }

    [Fact]
    public void BoundedReadRejectsListsAndArraysBeforeAllocatingThem() {
        CompoundTag root = new();
        root.Add("list", new ListTag {
            Value = [new ByteTag { Value = 1 }, new ByteTag { Value = 2 }, new ByteTag { Value = 3 }]
        });
        byte[] data = new byte[root.GetSize()];
        int offset = 0;
        NbtCodec.Write(root, data, ref offset);

        Assert.Throws<InvalidDataException>(() => {
            int readOffset = 0;
            NbtCodec.Read(data, ref readOffset, Options.LittleEndian, new NbtReadLimits(10, 10, 2, 10));
        });

        root = new CompoundTag();
        root.Add("bytes", new ByteArrayTag { Value = [1, 2, 3] });
        data = new byte[root.GetSize()];
        offset = 0;
        NbtCodec.Write(root, data, ref offset);
        Assert.Throws<InvalidDataException>(() => {
            int readOffset = 0;
            NbtCodec.Read(data, ref readOffset, Options.LittleEndian, new NbtReadLimits(10, 10, 10, 2));
        });
    }

    [Fact]
    public void BoundedReadLimitsTotalArrayElementsAcrossTags() {
        CompoundTag root = new();
        root.Add("first", new LongArrayTag { Value = [1, 2] });
        root.Add("second", new LongArrayTag { Value = [3, 4] });
        byte[] data = new byte[root.GetSize()];
        int offset = 0;
        NbtCodec.Write(root, data, ref offset);

        Assert.Throws<InvalidDataException>(() => {
            int readOffset = 0;
            NbtCodec.Read(data, ref readOffset, Options.LittleEndian, new NbtReadLimits(10, 10, 10, 3));
        });
    }
}
