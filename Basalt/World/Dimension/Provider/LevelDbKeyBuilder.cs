using System.Buffers.Binary;
using Basalt.Protocol.Types;

namespace Basalt.World.Dimension.Provider;

internal static class LevelDbKeyBuilder
{
    private const byte PrefixChunk = 0x2F;
    private const byte PrefixBlockList = 0x31;
    private const byte PrefixBlockStorage = 0x32;
    private const byte PrefixEntityList = 0x33;
    private const byte PrefixEntityStorage = 0x34;

    public static byte[] BuildChunkKey(int x, int z)
    {
        byte[] key = new byte[9];
        WriteChunkKey(key, x, z);
        return key;
    }

    public static byte[] BuildBlockStorageListKey(int x, int z)
    {
        byte[] key = new byte[9];
        WriteBlockStorageListKey(key, x, z);
        return key;
    }

    public static byte[] BuildBlockStorageKey(BlockPos pos)
    {
        byte[] key = new byte[13];
        WriteBlockStorageKey(key, pos);
        return key;
    }

    public static byte[] BuildEntityListKey(int x, int z)
    {
        byte[] key = new byte[9];
        WriteEntityListKey(key, x, z);
        return key;
    }

    public static byte[] BuildEntityStorageKey(long uniqueId)
    {
        byte[] key = new byte[9];
        WriteEntityStorageKey(key, uniqueId);
        return key;
    }

    public static void WriteChunkKey(Span<byte> key, int x, int z)
    {
        key[0] = PrefixChunk;
        BinaryPrimitives.WriteInt32LittleEndian(key[1..5], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[5..9], z);
    }

    public static void WriteBlockStorageListKey(Span<byte> key, int x, int z)
    {
        key[0] = PrefixBlockList;
        BinaryPrimitives.WriteInt32LittleEndian(key[1..5], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[5..9], z);
    }

    public static void WriteBlockStorageKey(Span<byte> key, BlockPos pos)
    {
        key[0] = PrefixBlockStorage;
        BinaryPrimitives.WriteInt32LittleEndian(key[1..5], pos.X);
        BinaryPrimitives.WriteInt32LittleEndian(key[5..9], pos.Y);
        BinaryPrimitives.WriteInt32LittleEndian(key[9..13], pos.Z);
    }

    public static void WriteEntityListKey(Span<byte> key, int x, int z)
    {
        key[0] = PrefixEntityList;
        BinaryPrimitives.WriteInt32LittleEndian(key[1..5], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[5..9], z);
    }

    public static void WriteEntityStorageKey(Span<byte> key, long uniqueId)
    {
        key[0] = PrefixEntityStorage;
        BinaryPrimitives.WriteInt64LittleEndian(key[1..9], uniqueId);
    }
}
