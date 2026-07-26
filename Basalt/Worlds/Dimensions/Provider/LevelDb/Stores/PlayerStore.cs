using System.Buffers;
using System.Text;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class PlayerStore {
    private static readonly TagOptions NbtOptions = new(Name: true, Type: true, VarInt: false);
    private readonly LevelDbDatabase _database;

    public PlayerStore(LevelDbDatabase database) {
        _database = database;
    }

    public IReadOnlyList<string> ListXuids() {
        List<string> xuids = [];
        using LevelDbIterator iterator = _database.CreateIterator();

        // In Vanilla Data the prefix is "player_server_" not a byte
        byte[] prefix = Encoding.UTF8.GetBytes("player_server_");
        iterator.Seek(prefix);

        while (iterator.Valid()) {
            ReadOnlySpan<byte> key = iterator.Key();
            if (key.Length <= prefix.Length || !key.StartsWith(prefix)) {
                break;
            }

            xuids.Add(Encoding.UTF8.GetString(key[prefix.Length..]));
            iterator.Next();
        }

        // Also scan legacy prefix (0x35) for migration.
        byte[] legacyPrefix = [0x35];
        iterator.Seek(legacyPrefix);

        while (iterator.Valid()) {
            ReadOnlySpan<byte> key = iterator.Key();
            if (key.Length == 0 || key[0] != 0x35) {
                break;
            }

            string xuid = Encoding.UTF8.GetString(key[1..]);
            if (!xuids.Contains(xuid)) {
                xuids.Add(xuid);
            }

            iterator.Next();
        }

        return xuids;
    }

    public CompoundTag? Load(string xuid) {
        byte[]? data = GetRaw(xuid);
        if (data is null) {
            return null;
        }

        int offset = 0;
        BinaryReader reader = new(data, ref offset);
        return ReadPlayerPayload(reader);
    }

    public byte[]? GetRaw(string xuid) {
        if (string.IsNullOrWhiteSpace(xuid)) {
            return null;
        }

        // Try vanilla key first.
        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildPlayerServerKey(xuid));
        if (data is { Length: > 0 }) {
            return data;
        }

        // Fallback to legacy key.
        data = _database.Get(LevelDbKeyBuilder.BuildLegacyPlayerStorageKey(xuid));
        return data is { Length: > 0 } ? data : null;
    }

    public static CompoundTag? LoadFromRaw(byte[] data) {
        int offset = 0;
        BinaryReader reader = new(data, ref offset);
        return ReadPlayerPayload(reader);
    }

    public void Save(string xuid, CompoundTag data) {
        if (string.IsNullOrWhiteSpace(xuid)) {
            throw new ArgumentException("Player xuid cannot be empty.", nameof(xuid));
        }

        byte[] payload = WritePlayerPayload(data);

        // Write vanilla key.
        _database.Put(LevelDbKeyBuilder.BuildPlayerServerKey(xuid), payload);

        // Delete legacy key.
        _database.Delete(LevelDbKeyBuilder.BuildLegacyPlayerStorageKey(xuid));
    }

    private static CompoundTag ReadPlayerPayload(BinaryReader reader) {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound) {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(reader, NbtOptions);
    }

    private static byte[] WritePlayerPayload(CompoundTag tag) {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true) {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try {
                NBT.WriteTag(writer, tag, NbtOptions);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 16 * 1024 * 1024) {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}







