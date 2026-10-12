using System.Security.Cryptography;
using System.Text.Json;

namespace BasaltBE.Client.Auth;

internal sealed record AuthCache(string RefreshToken, string PrivateKey);

internal static class AuthCacheFile {
    internal static async ValueTask<AuthCache?> LoadAsync(string path, CancellationToken cancellationToken) {
        if (!File.Exists(path)) return null;
        try {
            await using FileStream stream = File.OpenRead(path);
            AuthCache? cache = await JsonSerializer.DeserializeAsync<AuthCache>(
                stream,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            if (cache is not { RefreshToken.Length: > 0, PrivateKey.Length: > 0 }) return null;
            using ECDsa key = ECDsa.Create();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(cache.PrivateKey), out _);

            return key.KeySize == 384 ? cache : null;
        }
        catch (Exception error) when (error is JsonException or CryptographicException or FormatException) {
            return null;
        }
    }

    internal static async ValueTask SaveAsync(string path, AuthCache cache, CancellationToken cancellationToken) {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null) Directory.CreateDirectory(directory);
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous
            )) {
                await JsonSerializer.SerializeAsync(
                    stream,
                    cache,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
            }

            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(temporaryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporaryPath, path, true);
        }
        finally {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
