using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Network.Login;
using BasaltBE.NetherNet.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class LoginTokenParserTests {
    [Fact]
    public void ParsePlayerUuidReturnsEmptyWhenSelfSignedIdIsAbsent() =>
        Assert.Equal(Guid.Empty, LoginAuthenticator.ParsePlayerUuid(string.Empty));

    [Fact]
    public void ParsePlayerUuidReadsSelfSignedId() {
        Guid expected = Guid.NewGuid();

        Assert.Equal(expected, LoginAuthenticator.ParsePlayerUuid(expected.ToString()));
    }

    [Fact]
    public void ParsePlayerUuidReturnsEmptyForMalformedSelfSignedId() =>
        Assert.Equal(Guid.Empty, LoginAuthenticator.ParsePlayerUuid("not-a-uuid"));

    [Fact]
    public void ReadClientPublicKeyVerifiesClientToken() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        byte[] publicKey = clientKey.ExportSubjectPublicKeyInfo();
        string header = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new {
            alg = "ES384",
            x5u = Base64Url.Encode(publicKey)
        }));
        string payload = Base64Url.Encode(Encoding.UTF8.GetBytes("{}"));
        string signingInput = $"{header}.{payload}";
        byte[] signature = clientKey.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        byte[] connectionRequest = CreateConnectionRequest("{\"AuthenticationType\":0,\"Token\":\"\"}", $"{signingInput}.{Base64Url.Encode(signature)}");

        ECParameters parsed = LoginTokenParser.Parse(connectionRequest).Client.PublicKey;
        ECParameters expected = clientKey.ExportParameters(false);

        Assert.Equal(expected.Q.X, parsed.Q.X);
        Assert.Equal(expected.Q.Y, parsed.Q.Y);
    }

    [Fact]
    public void ReadClientPublicKeyRejectsInvalidSignature() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        byte[] publicKey = clientKey.ExportSubjectPublicKeyInfo();
        string header = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new {
            alg = "ES384",
            x5u = Base64Url.Encode(publicKey)
        }));
        string payload = Base64Url.Encode(Encoding.UTF8.GetBytes("{}"));
        string clientToken = $"{header}.{payload}.{Base64Url.Encode(new byte[96])}";

        Assert.Throws<CryptographicException>(() => LoginTokenParser.Parse(CreateConnectionRequest("{\"AuthenticationType\":0,\"Token\":\"\"}", clientToken)));
    }

    [Fact]
    public void ParseReadsLargeSkinDataFromPooledClientPayload() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        ClientData client = new() {
            SkinData = new byte[128 * 128 * 4],
            SkinImageWidth = 128,
            SkinImageHeight = 128
        };
        byte[] clientJson = JsonSerializer.SerializeToUtf8Bytes(client, LoginJson.Options);
        string header = Base64Url.Encode(JsonSerializer.SerializeToUtf8Bytes(new {
            alg = "ES384",
            x5u = Base64Url.Encode(clientKey.ExportSubjectPublicKeyInfo())
        }));
        string payload = Base64Url.Encode(clientJson);
        string signingInput = $"{header}.{payload}";
        byte[] signature = clientKey.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA384,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        string clientToken = $"{signingInput}.{Base64Url.Encode(signature)}";
        byte[] connectionRequest = CreateConnectionRequest(
            "{\"AuthenticationType\":0,\"Token\":\"\"}", clientToken);

        ClientData parsed = LoginTokenParser.Parse(connectionRequest).Client.Client;

        Assert.Equal(client.SkinData, parsed.SkinData);
    }

    [Fact]
    public void Base64UrlDecodesIntoCallerBuffer() {
        byte[] expected = [0xFB, 0xFF, 0xF0, 0x00];
        string[] encodedValues = [
            Base64Url.Encode(expected),
            Convert.ToBase64String(expected)
        ];

        foreach (string encodedValue in encodedValues) {
            byte[] encoded = Encoding.ASCII.GetBytes(encodedValue);
            byte[] output = new byte[encoded.Length];

            int written = Base64Url.Decode(encoded, output);

            Assert.Equal(expected, output.AsSpan(0, written).ToArray());
        }
    }

    private static byte[] CreateConnectionRequest(string identityToken, string clientToken) {
        byte[] identity = Encoding.UTF8.GetBytes(identityToken);
        byte[] client = Encoding.UTF8.GetBytes(clientToken);
        byte[] request = GC.AllocateUninitializedArray<byte>(identity.Length + client.Length + 8);
        BinaryPrimitives.WriteInt32LittleEndian(request, identity.Length);
        identity.CopyTo(request.AsSpan(4));
        int clientOffset = identity.Length + 4;
        BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(clientOffset), client.Length);
        client.CopyTo(request.AsSpan(clientOffset + 4));
        return request;
    }
}
