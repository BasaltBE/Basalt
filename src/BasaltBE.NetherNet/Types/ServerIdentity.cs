using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BasaltBE.NetherNet.Types;

public sealed class ServerIdentity : IServerIdentity {
    private readonly ECDsa key;
    private readonly Lock sync = new();
    private bool disposed;

    public ServerIdentity(string keyPath) {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);
        string path = Path.GetFullPath(keyPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        if (File.Exists(path)) {
            key.ImportFromPem(File.ReadAllText(path));
            if (key.KeySize != 384)
                throw new InvalidDataException("NetherNet identity key must use P-384.");
        }
        else {
            try {
                using FileStream file = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using StreamWriter writer = new(file);
                writer.Write(key.ExportPkcs8PrivateKeyPem());
            }
            catch (IOException) when (File.Exists(path)) {
                key.ImportFromPem(File.ReadAllText(path));
            }
        }
    }

    public string AddAssertion(string answerSdp) {
        ArgumentException.ThrowIfNullOrEmpty(answerSdp);
        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed, this);
            List<(string Algorithm, string Digest)> fingerprints = [];
            foreach (string line in answerSdp.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
                if (!line.StartsWith("a=fingerprint:", StringComparison.Ordinal)) continue;
                ReadOnlySpan<char> value = line.AsSpan("a=fingerprint:".Length);
                int separator = value.IndexOf(' ');
                if (separator <= 0 || separator == value.Length - 1)
                    throw new InvalidDataException("SDP has an invalid fingerprint.");
                fingerprints.Add((value[..separator].ToString(), value[(separator + 1)..].Trim().ToString()));
            }
            if (fingerprints.Count == 0) throw new InvalidDataException("SDP has no fingerprints.");

            byte[] fingerprintJson;
            using (MemoryStream json = new()) {
                using (Utf8JsonWriter writer = new(json)) {
                    writer.WriteStartObject();
                    writer.WriteStartArray("fingerprint");
                    foreach ((string algorithm, string digest) in fingerprints) {
                        writer.WriteStartObject();
                        writer.WriteString("algorithm", algorithm);
                        writer.WriteString("digest", digest);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                fingerprintJson = json.ToArray();
            }

            byte[] jwk = JsonSerializer.SerializeToUtf8Bytes(new {
                alg = "ES384",
                typ = "JWT"
            });
            byte[] jwtPayload = JsonSerializer.SerializeToUtf8Bytes(new {
                cpk = new { kty = "EC", crv = "P-384", x = GetCoordinate("x"), y = GetCoordinate("y") },
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddYears(10).ToUnixTimeSeconds()
            });
            string token = SignJwt(jwk, jwtPayload);
            string detached = SignDetached(fingerprintJson);
            string assertion = JsonSerializer.Serialize(new { token, fingerprints = detached });
            string envelope = JsonSerializer.Serialize(new { idp = new { domain = "self", protocol = "default" }, assertion });
            string identity = Convert.ToBase64String(Encoding.UTF8.GetBytes(envelope));
            int media = answerSdp.IndexOf("m=", StringComparison.Ordinal);
            if (media < 0) throw new InvalidDataException("SDP has no media section.");
            int lineStart = media == 0 || answerSdp[media - 1] == '\n' ? media : answerSdp.LastIndexOf('\n', media) + 1;
            return answerSdp[..lineStart] + "a=identity:" + identity + "\r\n" + answerSdp[lineStart..];
        }
    }

    public void Dispose() {
        lock (sync) {
            if (disposed) return;
            disposed = true;
            key.Dispose();
        }
    }

    private string GetCoordinate(string coordinate) {
        ECParameters parameters = key.ExportParameters(false);
        return Base64Url.Encode(coordinate == "x" ? parameters.Q.X! : parameters.Q.Y!);
    }

    private string SignJwt(byte[] header, byte[] payload) {
        string encodedHeader = Base64Url.Encode(header);
        string encodedPayload = Base64Url.Encode(payload);
        byte[] content = Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
        byte[] signature = key.SignData(content, HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{encodedHeader}.{encodedPayload}.{Base64Url.Encode(signature)}";
    }

    private string SignDetached(byte[] payload) {
        string header = Base64Url.Encode("{\"alg\":\"ES384\"}"u8);
        string encodedPayload = Base64Url.Encode(payload);
        byte[] content = Encoding.ASCII.GetBytes($"{header}.{encodedPayload}");
        byte[] signature = key.SignData(content, HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        return $"{header}..{Base64Url.Encode(signature)}";
    }
}

