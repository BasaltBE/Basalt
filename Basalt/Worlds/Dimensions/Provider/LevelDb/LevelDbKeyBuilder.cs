using System.Buffers.Binary;
using System.Text;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal static class LevelDbKeyBuilder
{
    private const byte PrefixChunk = 0x2F;
    private const byte PrefixBlockList = 0x31;
    private const byte PrefixBlockStorage = 0x32;
    private const byte PrefixEntityList = 0x33;
    private const byte PrefixEntityStorage = 0x34;
    private const byte PrefixPlayerStorage = 0x35;
    private const byte PrefixSpawnPosition = 0x36;

    private const byte TagData3D = 0x2B;
    private const byte TagVersion = 0x2C;
    private const byte TagData2D = 0x2D;
    private const byte TagSubChunkPrefix = 0x2F;
    private const byte TagBlockEntity = 0x31;

    public static byte[] BuildChunkKey(int x, int z)
    {
        byte[] key = new byte[9];
        WriteChunkKey(key, x, z);
        return key;
    }

    public static byte[] BuildChunkKey(DimensionType dimensionType, int x, int z)
    {
        byte[] key = new byte[10];
        WriteChunkKey(key, dimensionType, x, z);
        return key;
    }

    public static byte[] BuildBlockStorageListKey(int x, int z)
    {
        byte[] key = new byte[9];
        WriteBlockStorageListKey(key, x, z);
        return key;
    }

    public static byte[] BuildBlockStorageListKey(DimensionType dimensionType, int x, int z)
    {
        byte[] key = new byte[10];
        WriteBlockStorageListKey(key, dimensionType, x, z);
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

    public static byte[] BuildEntityListKey(DimensionType dimensionType, int x, int z)
    {
        byte[] key = new byte[10];
        WriteEntityListKey(key, dimensionType, x, z);
        return key;
    }

    public static byte[] BuildEntityStorageKey(long uniqueId)
    {
        byte[] key = new byte[9];
        WriteEntityStorageKey(key, uniqueId);
        return key;
    }

    public static byte[] BuildPlayerStorageKey(string xuid)
    {
        byte[] idBytes = Encoding.UTF8.GetBytes(xuid);
        byte[] key = new byte[idBytes.Length + 1];
        key[0] = PrefixPlayerStorage;
        idBytes.CopyTo(key, 1);
        return key;
    }

    public static byte[] BuildTagKey(DimensionType dimensionType, int x, int z, byte tag)
    {
        if (dimensionType == DimensionType.Overworld)
        {
            byte[] key = new byte[9];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            key[8] = tag;
            return key;
        }
        else
        {
            byte[] key = new byte[13];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(8, 4), (int)dimensionType);
            key[12] = tag;
            return key;
        }
    }

    public static byte[] BuildSubChunkKey(DimensionType dimensionType, int x, int z, sbyte index)
    {
        if (dimensionType == DimensionType.Overworld)
        {
            byte[] key = new byte[10];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            key[8] = TagSubChunkPrefix;
            key[9] = (byte)index;
            return key;
        }
        else
        {
            byte[] key = new byte[14];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(8, 4), (int)dimensionType);
            key[12] = TagSubChunkPrefix;
            key[13] = (byte)index;
            return key;
        }
    }

    public static byte[] BuildVersionKey(DimensionType dimensionType, int x, int z)
    {
        return BuildTagKey(dimensionType, x, z, TagVersion);
    }

    public static byte[] BuildData3DKey(DimensionType dimensionType, int x, int z)
    {
        return BuildTagKey(dimensionType, x, z, TagData3D);
    }

    public static byte[] BuildData2DKey(DimensionType dimensionType, int x, int z)
    {
        return BuildTagKey(dimensionType, x, z, TagData2D);
    }

    public static byte[] BuildBlockEntityKey(DimensionType dimensionType, int x, int z)
    {
        return BuildTagKey(dimensionType, x, z, TagBlockEntity);
    }

    public static void WriteChunkKey(Span<byte> key, DimensionType dimensionType, int x, int z)
    {
        key[0] = PrefixChunk;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key[2..6], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[6..10], z);
    }

    public static void WriteChunkKey(Span<byte> key, int x, int z)
    {
        key[0] = PrefixChunk;
        BinaryPrimitives.WriteInt32LittleEndian(key[1..5], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[5..9], z);
    }

    public static void WriteBlockStorageListKey(Span<byte> key, DimensionType dimensionType, int x, int z)
    {
        key[0] = PrefixBlockList;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key[2..6], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[6..10], z);
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

    public static void WriteEntityListKey(Span<byte> key, DimensionType dimensionType, int x, int z)
    {
        key[0] = PrefixEntityList;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key[2..6], x);
        BinaryPrimitives.WriteInt32LittleEndian(key[6..10], z);
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

    public static byte[] BuildSpawnPositionKey(DimensionType dimensionType)
    {
        byte[] key = new byte[2];
        key[0] = PrefixSpawnPosition;
        key[1] = (byte)dimensionType;
        return key;
    }
}







