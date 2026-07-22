using System.Buffers.Binary;
using System.Text;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal static class LevelDbKeyBuilder {
    private const byte TagData3D = 0x2B;
    private const byte TagVersion = 0x2C;
    private const byte TagData2D = 0x2D;
    private const byte TagSubChunkPrefix = 0x2F;
    private const byte TagBlockEntity = 0x31;

    private static readonly byte[] ActorPrefixBytes = "actorprefix"u8.ToArray();
    private static readonly byte[] DigpBytes = "digp"u8.ToArray();
    private static readonly byte[] LocalPlayerBytes = "~local_player"u8.ToArray();
    private static readonly byte[] PlayerServerBytes = "player_server_"u8.ToArray();

    private const byte LegacyPrefixChunk = 0x2F;
    private const byte LegacyPrefixBlockList = 0x31;
    private const byte LegacyPrefixBlockStorage = 0x32;
    private const byte LegacyPrefixEntityList = 0x33;
    private const byte LegacyPrefixEntityStorage = 0x34;
    private const byte LegacyPrefixPlayerStorage = 0x35;
    private const byte LegacyPrefixSpawnPosition = 0x36;

    public static byte[] BuildLegacyChunkKey(int x, int z) {
        byte[] key = new byte[9];
        key[0] = LegacyPrefixChunk;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(1, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(5, 4), z);
        return key;
    }

    public static byte[] BuildLegacyChunkKey(DimensionType dimensionType, int x, int z) {
        byte[] key = new byte[10];
        key[0] = LegacyPrefixChunk;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(2, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(6, 4), z);
        return key;
    }

    public static byte[] BuildLegacyBlockStorageListKey(int x, int z) {
        byte[] key = new byte[9];
        key[0] = LegacyPrefixBlockList;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(1, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(5, 4), z);
        return key;
    }

    public static byte[] BuildLegacyBlockStorageListKey(DimensionType dimensionType, int x, int z) {
        byte[] key = new byte[10];
        key[0] = LegacyPrefixBlockList;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(2, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(6, 4), z);
        return key;
    }

    public static byte[] BuildLegacyEntityListKey(int x, int z) {
        byte[] key = new byte[9];
        key[0] = LegacyPrefixEntityList;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(1, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(5, 4), z);
        return key;
    }

    public static byte[] BuildLegacyEntityListKey(DimensionType dimensionType, int x, int z) {
        byte[] key = new byte[10];
        key[0] = LegacyPrefixEntityList;
        key[1] = (byte)dimensionType;
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(2, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(6, 4), z);
        return key;
    }

    public static byte[] BuildLegacyEntityStorageKey(long uniqueId) {
        byte[] key = new byte[9];
        key[0] = LegacyPrefixEntityStorage;
        BinaryPrimitives.WriteInt64LittleEndian(key.AsSpan(1, 8), uniqueId);
        return key;
    }

    public static byte[] BuildLegacyPlayerStorageKey(string xuid) {
        byte[] idBytes = Encoding.UTF8.GetBytes(xuid);
        byte[] key = new byte[idBytes.Length + 1];
        key[0] = LegacyPrefixPlayerStorage;
        idBytes.CopyTo(key, 1);
        return key;
    }

    public static byte[] BuildLegacySpawnPositionKey(DimensionType dimensionType) {
        return [LegacyPrefixSpawnPosition, (byte)dimensionType];
    }


    public static byte[] BuildTagKey(DimensionType dimensionType, int x, int z, byte tag) {
        if (dimensionType == DimensionType.Overworld) {
            byte[] key = new byte[9];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            key[8] = tag;
            return key;
        }

        byte[] dimKey = new byte[13];
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(0, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(4, 4), z);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(8, 4), (int)dimensionType);
        dimKey[12] = tag;
        return dimKey;
    }

    public static byte[] BuildSubChunkKey(DimensionType dimensionType, int x, int z, sbyte index) {
        if (dimensionType == DimensionType.Overworld) {
            byte[] key = new byte[10];
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(0, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), z);
            key[8] = TagSubChunkPrefix;
            key[9] = (byte)index;
            return key;
        }

        byte[] dimKey = new byte[14];
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(0, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(4, 4), z);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(8, 4), (int)dimensionType);
        dimKey[12] = TagSubChunkPrefix;
        dimKey[13] = (byte)index;
        return dimKey;
    }

    public static byte[] BuildVersionKey(DimensionType dimensionType, int x, int z) {
        return BuildTagKey(dimensionType, x, z, TagVersion);
    }

    public static byte[] BuildData3DKey(DimensionType dimensionType, int x, int z) {
        return BuildTagKey(dimensionType, x, z, TagData3D);
    }

    public static byte[] BuildData2DKey(DimensionType dimensionType, int x, int z) {
        return BuildTagKey(dimensionType, x, z, TagData2D);
    }

    public static byte[] BuildBlockEntityKey(DimensionType dimensionType, int x, int z) {
        return BuildTagKey(dimensionType, x, z, TagBlockEntity);
    }

    public static byte[] BuildActorPrefixKey(long uniqueId) {
        byte[] key = new byte[ActorPrefixBytes.Length + 8];
        ActorPrefixBytes.CopyTo(key, 0);
        BinaryPrimitives.WriteInt64LittleEndian(key.AsSpan(ActorPrefixBytes.Length, 8), uniqueId);
        return key;
    }

    public static byte[] BuildDigpKey(DimensionType dimensionType, int x, int z) {
        if (dimensionType == DimensionType.Overworld) {
            byte[] key = new byte[12];
            DigpBytes.CopyTo(key, 0);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(4, 4), x);
            BinaryPrimitives.WriteInt32LittleEndian(key.AsSpan(8, 4), z);
            return key;
        }

        byte[] dimKey = new byte[16];
        DigpBytes.CopyTo(dimKey, 0);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(4, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(8, 4), z);
        BinaryPrimitives.WriteInt32LittleEndian(dimKey.AsSpan(12, 4), (int)dimensionType);
        return dimKey;
    }

    public static int WriteDigpKey(Span<byte> destination, DimensionType dimensionType, int x, int z) {
        DigpBytes.CopyTo(destination);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(4, 4), x);
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(8, 4), z);

        if (dimensionType == DimensionType.Overworld) {
            return 12;
        }

        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(12, 4), (int)dimensionType);
        return 16;
    }

    public static byte[] BuildLocalPlayerKey() {
        return LocalPlayerBytes;
    }

    public static byte[] BuildPlayerServerKey(string xuid) {
        byte[] idBytes = Encoding.UTF8.GetBytes(xuid);
        byte[] key = new byte[PlayerServerBytes.Length + idBytes.Length];
        PlayerServerBytes.CopyTo(key, 0);
        idBytes.CopyTo(key, PlayerServerBytes.Length);
        return key;
    }
}







