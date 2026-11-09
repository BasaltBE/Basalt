using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BasaltBE.Client.Login;

internal static class BedrockNethernetIdentity {
    internal static string AddAssertion(string offerSdp, string gameServerToken, string privateKey) {
        using ECDsa key = ECDsa.Create();
        key.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
        List<(string Algorithm, string Digest)> fingerprints = [];
        foreach (string line in offerSdp.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)) {
            if (!line.StartsWith("a=fingerprint:", StringComparison.Ordinal)) {
                continue;
            }

            ReadOnlySpan<char> value = line.AsSpan("a=fingerprint:".Length);
            int separator = value.IndexOf(' ');
            if (separator <= 0 || separator == value.Length - 1) {
                throw new InvalidDataException("SDP has an invalid fingerprint.");
            }

            fingerprints.Add((value[..separator].ToString(), value[(separator + 1)..].Trim().ToString()));
        }
        if (fingerprints.Count == 0) {
            throw new InvalidDataException("SDP has no fingerprints.");
        }

        using MemoryStream json = new();
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

        string encodedHeader = Base64Url("{\"alg\":\"ES384\"}"u8);
        string encodedPayload = Base64Url(json.ToArray());
        byte[] signedContent = Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
        byte[] signature = key.SignData(
            signedContent,
            HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation
        );
        string detachedSignature = $"{encodedHeader}..{Base64Url(signature)}";
        string assertion = JsonSerializer.Serialize(new { token = gameServerToken, fingerprints = detachedSignature });
        string envelope = JsonSerializer.Serialize(new {
            idp = new { domain = "https://authorization.franchise.minecraft-services.net/", protocol = "default" },
            assertion
        });
        string identity = Convert.ToBase64String(Encoding.UTF8.GetBytes(envelope));
        int media = offerSdp.IndexOf("m=", StringComparison.Ordinal);
        if (media < 0) {
            throw new InvalidDataException("SDP has no media section.");
        }

        int lineStart = media == 0 || offerSdp[media - 1] == '\n' ? media : offerSdp.LastIndexOf('\n', media) + 1;
        return offerSdp[..lineStart] + "a=identity:" + identity + "\r\n" + offerSdp[lineStart..];
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
