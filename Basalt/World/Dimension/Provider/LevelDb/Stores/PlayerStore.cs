using System.Buffers;
using Basalt.Protocol.Io;
using Basalt.Protocol.Nbt;
using LevelDB;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.World.Dimension.Provider;

internal sealed class PlayerStore
{
    private static readonly TagOptions NbtOptions = new(Name: true, Type: true, VarInt: false);
    private readonly DB _database;

    public PlayerStore(DB database)
    {
        _database = database;
    }

    public CompoundTag? Load(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return null;
        }

        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid));
        if (data is null || data.Length == 0)
        {
            return null;
        }

        int offset = 0;
        BinaryReader reader = new(data, ref offset);
        return ReadPlayerPayload(reader);
    }

    public void Save(string xuid, CompoundTag data)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            throw new ArgumentException("Player xuid cannot be empty.", nameof(xuid));
        }

        _database.Put(LevelDbKeyBuilder.BuildPlayerStorageKey(xuid), WritePlayerPayload(data));
    }

    private static CompoundTag ReadPlayerPayload(BinaryReader reader)
    {
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound)
        {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(reader, NbtOptions);
    }

    private static byte[] WritePlayerPayload(CompoundTag tag)
    {
        int size = 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try
            {
                NBT.WriteTag(writer, tag, NbtOptions);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 16 * 1024 * 1024)
                {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}
