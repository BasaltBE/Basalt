namespace Basalt.Core.Player;

using System.Buffers;
using System.Text;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

using BedrockProtocol.Nbt;

public sealed class PlayerDataStore {
    private static readonly TagOptions NbtOptions = new(Name: true, Type: true, VarInt: false);
    private readonly string _path;

    public PlayerDataStore(string path) {
        _path = path;
        Directory.CreateDirectory(_path);
    }

    public CompoundTag? Load(string xuid) {
        string path = GetPath(xuid);
        if (!File.Exists(path)) {
            return null;
        }

        return Deserialize(File.ReadAllBytes(path));
    }

    public void Save(string xuid, CompoundTag data) {
        string path = GetPath(xuid);
        string temporaryPath = path + ".tmp";
        File.WriteAllBytes(temporaryPath, Serialize(data));
        File.Move(temporaryPath, path, true);
    }

    internal static CompoundTag Deserialize(byte[] data) {
        int offset = 0;
        BinaryReader reader = new(data, ref offset);
        TagType type = (TagType)reader.ReadInt8();
        if (type != TagType.Compound) {
            throw new InvalidOperationException($"Expected Compound tag, got {type}.");
        }

        return CompoundTag.Read(reader, NbtOptions);
    }

    private string GetPath(string xuid) {
        if (string.IsNullOrWhiteSpace(xuid)) {
            throw new ArgumentException("Player xuid cannot be empty.", nameof(xuid));
        }

        return Path.Combine(_path, Convert.ToHexString(Encoding.UTF8.GetBytes(xuid)) + ".dat");
    }

    internal static byte[] Serialize(CompoundTag tag) {
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
            catch (Exception exception) when (exception is ArgumentOutOfRangeException or IndexOutOfRangeException) {
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
