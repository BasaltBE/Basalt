using System.Text.Json;

namespace Basalt.Waterfall.Auth;

public class AuthCache
{
    private readonly string _filePath;

    public AuthCache(string cacheDir, string username)
    {
        _filePath = Path.Combine(cacheDir, $"{username}.json");
    }

    public TokenCache Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new TokenCache();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<TokenCache>(json) ?? new TokenCache();
        }
        catch
        {
            return new TokenCache();
        }
    }

    public void Save(TokenCache cache)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(cache, options);
        File.WriteAllText(_filePath, json);
    }

    public void Clear()
    {
        Save(new TokenCache());
    }

    public static bool IsTokenValid(long expiresAt)
    {
        if (expiresAt == 0) return false;
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < expiresAt - 60_000;
    }
}
