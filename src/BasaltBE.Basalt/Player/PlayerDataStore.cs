using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Logging;
using BasaltBE.Nbt.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.PlayerData;

internal sealed class PlayerDataStore {
    private const int DataVersion = 1;
    private readonly string directory;

    internal PlayerDataStore(string directory) {
        this.directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(this.directory);
    }

    internal CompoundTag? Load(string xuid) {
        string path = GetPath(xuid);
        if (!File.Exists(path))
            return null;

        try {
            byte[] data = File.ReadAllBytes(path);
            int offset = 0;
            if (BasaltBE.Nbt.Nbt.Read(data, ref offset) is not CompoundTag playerData || offset != data.Length ||
                playerData.Get<IntTag>("DataVersion")?.Value != DataVersion ||
                playerData.Get<StringTag>("Xuid")?.Value != xuid)
                throw new InvalidDataException("Player data has an invalid format or identity.");

            return playerData;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) {
            Logger.Scoped("PlayerData").Warn($"Could not load player data for {xuid}: {error.Message}");
            return null;
        }
    }

    internal void Save(Player player) {
        CompoundTag data = player.SaveData();
        data.Add("DataVersion", new IntTag { Value = DataVersion });
        data.Add("Xuid", new StringTag { Value = player.Xuid });
        using BinaryStream stream = BinaryStream.Rent(data.GetSize());
        BinaryWriter writer = stream.GetWriter();
        BasaltBE.Nbt.Nbt.Write(data, ref writer);

        string path = GetPath(player.Xuid);
        string temporaryPath = path + ".tmp";
        try {
            File.WriteAllBytes(temporaryPath, stream.Buffer.Span[..stream.Offset]);
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) {
            Logger.Scoped("PlayerData").Warn($"Could not save player data for {player.Username}: {error.Message}");
        }
    }

    private string GetPath(string xuid) {
        ArgumentException.ThrowIfNullOrWhiteSpace(xuid);
        foreach (char character in xuid) {
            if (!char.IsAsciiDigit(character))
                throw new ArgumentException("Player XUID must contain only digits.", nameof(xuid));
        }

        return Path.Combine(directory, xuid + ".dat");
    }
}
