using System.Collections.Concurrent;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Logging;
using BasaltBE.Nbt.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.PlayerData;

internal sealed class PlayerDataStore : IDisposable {
    private const int DataVersion = 1;
    private const int MaximumPendingSaves = 4096;
    private const long MaximumPendingBytes = 256L * 1024 * 1024;
    private readonly string directory;
    private readonly Lock sync = new();
    private readonly BlockingCollection<PlayerDataWrite> writes = new(new ConcurrentQueue<PlayerDataWrite>(), MaximumPendingSaves);
    private readonly Dictionary<string, PlayerDataWrite> pending = new(StringComparer.Ordinal);
    private readonly Thread worker;
    private long pendingBytes;
    private bool disposed;

    internal PlayerDataStore(string directory) {
        this.directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(this.directory);
        worker = new Thread(Write) {
            IsBackground = true,
            Name = "Basalt player data"
        };
        worker.Start();
    }

    internal CompoundTag? Load(string xuid) {
        string path = GetPath(xuid);
        byte[]? pendingData;
        lock (sync)
            pendingData = pending.TryGetValue(xuid, out PlayerDataWrite? save) && save is not null ? save.Data : null;
        if (pendingData is null && !File.Exists(path))
            return null;

        try {
            byte[] data = pendingData ?? File.ReadAllBytes(path);
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

        byte[] bytes = stream.Buffer.Span[..stream.Offset].ToArray();
        PlayerDataWrite save = new(player.Xuid, GetPath(player.Xuid), bytes);
        lock (sync) {
            if (disposed || pendingBytes + bytes.Length > MaximumPendingBytes || !writes.TryAdd(save)) {
                Logger.Scoped("PlayerData").Warn($"Could not queue a save for {player.Username}; the player-data queue is full.");
                return;
            }

            pending[player.Xuid] = save;
            pendingBytes += bytes.Length;
        }
    }

    private void Write() {
        foreach (PlayerDataWrite save in writes.GetConsumingEnumerable()) {
            bool current;
            lock (sync)
                current = pending.TryGetValue(save.Xuid, out PlayerDataWrite? latest) && ReferenceEquals(latest, save);

            if (current) {
                try {
                    string temporaryPath = save.Path + ".tmp";
                    File.WriteAllBytes(temporaryPath, save.Data);
                    File.Move(temporaryPath, save.Path, overwrite: true);
                }
                catch (Exception error) {
                    Logger.Scoped("PlayerData").Warn($"Could not save player data for {save.Xuid}: {error.Message}");
                }
            }

            lock (sync) {
                pendingBytes -= save.Data.Length;
                if (pending.TryGetValue(save.Xuid, out PlayerDataWrite? latest) && ReferenceEquals(latest, save))
                    pending.Remove(save.Xuid);
            }
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

    public void Dispose() {
        lock (sync) {
            if (disposed)
                return;
            disposed = true;
        }

        writes.CompleteAdding();
        worker.Join();
        writes.Dispose();
    }

    private sealed record PlayerDataWrite(string Xuid, string Path, byte[] Data);
}
