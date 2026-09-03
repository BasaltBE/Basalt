using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Basalt.Core.Nethernet;

public sealed class ServerIdentity : IDisposable {
    private readonly ECDsa _key;
    private readonly string _publicKey;
    private readonly string _domain;
    private readonly TimeSpan _tokenLifetime;

    public ServerIdentity(
        ECDsa key,
        string domain,
        TimeSpan? tokenLifetime = null) {
        _key = key ?? throw new ArgumentNullException(nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        _domain = domain;
        _tokenLifetime = tokenLifetime ?? TimeSpan.FromHours(1);
        _publicKey = Convert.ToBase64String(_key.ExportSubjectPublicKeyInfo());
    }

    public static ServerIdentity Generate(string domain = "self") {
        ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        return new ServerIdentity(key, domain);
    }

    public static ServerIdentity LoadOrGenerate(string path, string domain = "self") {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (File.Exists(path)) {
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            key.ImportFromPem(File.ReadAllText(path));
            return new ServerIdentity(key, domain);
        }

        ServerIdentity identity = Generate(domain);
        File.WriteAllText(path, identity._key.ExportPkcs8PrivateKeyPem());
        return identity;
    }

    public string AddIdentity(string answer) {
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);

        string[] lines = answer.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        List<string> fingerprints = [];
        foreach (string line in lines) {
            if (line.StartsWith("a=fingerprint:", StringComparison.Ordinal)) {
                fingerprints.Add(line[14..]);
            }
        }

        if (fingerprints.Count == 0) {
            throw new InvalidDataException("The SDP answer does not contain a DTLS fingerprint.");
        }

        string fingerprintJson = BuildFingerprintJson(fingerprints);
        string detached = SignDetached(fingerprintJson);
        string token = CreateToken();
        string assertion = JsonSerializer.Serialize(new {
            fingerprints = detached,
            token
        });
        string envelope = JsonSerializer.Serialize(new {
            assertion,
            idp = new {
                domain = _domain,
                protocol = "default"
            }
        });
        string identity = Convert.ToBase64String(Encoding.UTF8.GetBytes(envelope));

        int mediaLine = Array.FindIndex(lines, line => line.StartsWith("m=", StringComparison.Ordinal));
        if (mediaLine < 0) {
            throw new InvalidDataException("The SDP answer does not contain a media section.");
        }

        List<string> result = [.. lines];
        result.Insert(mediaLine, $"a=identity:{identity}");
        return string.Join("\r\n", result);
    }

    public void Dispose() {
        _key.Dispose();
    }

    private string CreateToken() {
        long issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new {
            alg = "ES384",
            x5u = _publicKey
        }));
        string claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new {
            exp = issuedAt + checked((long)_tokenLifetime.TotalSeconds),
            iat = issuedAt,
            cpk = _publicKey
        }));
        byte[] signature = _key.SignData(
            Encoding.ASCII.GetBytes($"{header}.{claims}"),
            HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{header}.{claims}.{Base64Url(signature)}";
    }

    private string SignDetached(string fingerprintJson) {
        const string header = "eyJhbGciOiJFUzM4NCJ9";
        byte[] signature = _key.SignData(
            Encoding.ASCII.GetBytes($"{header}.{Base64Url(Encoding.UTF8.GetBytes(fingerprintJson))}"),
            HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{header}..{Base64Url(signature)}";
    }

    private static string BuildFingerprintJson(List<string> fingerprints) {
        StringBuilder builder = new("{\"fingerprint\":[");
        for (int index = 0; index < fingerprints.Count; index++) {
            if (index > 0) {
                builder.Append(',');
            }

            string[] parts = fingerprints[index].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) {
                throw new InvalidDataException("The SDP contains an invalid DTLS fingerprint.");
            }

            builder.Append("{\"algorithm\":");
            builder.Append(JsonSerializer.Serialize(parts[0]));
            builder.Append(",\"digest\":");
            builder.Append(JsonSerializer.Serialize(parts[1]));
            builder.Append('}');
        }

        return builder.Append("]}").ToString();
    }

    private static string Base64Url(byte[] bytes) {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
