using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Network;
using BasaltBE.NetherNet.Types;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class BedrockEncryptionTests {
    [Fact]
    public void EncryptAndDecryptPreserveMultiplePackets() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using BedrockEncryption encryption = BedrockEncryption.Create(clientKey.ExportParameters(false), out _);
        byte[][] messages = [Encoding.UTF8.GetBytes("first packet"), new byte[1024], new byte[64 * 1024], Encoding.UTF8.GetBytes("third")];

        foreach (byte[] message in messages)
            Assert.Equal(message, encryption.Decrypt(encryption.Encrypt(message)));
    }

    [Fact]
    public void EncryptAndDecryptWriteIntoProvidedBuffers() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using BedrockEncryption encryption = BedrockEncryption.Create(clientKey.ExportParameters(false), out _);
        byte[] message = RandomNumberGenerator.GetBytes(64 * 1024);
        byte[] ciphertext = new byte[message.Length + 8];
        byte[] plaintext = new byte[message.Length];

        int ciphertextLength = encryption.Encrypt(message, ciphertext);
        int plaintextLength = encryption.Decrypt(ciphertext.AsSpan(0, ciphertextLength), plaintext);

        Assert.Equal(message.Length + 8, ciphertextLength);
        Assert.Equal(message.Length, plaintextLength);
        Assert.Equal(message, plaintext);
    }

    [Fact]
    public void ServerAndClientExchangeEncryptedPackets() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        ECParameters clientParameters = clientKey.ExportParameters(true);
        using BedrockEncryption server = BedrockEncryption.Create(clientKey.ExportParameters(false), out string token);
        using BedrockEncryption client = CreateClientEncryption(clientParameters, token);
        byte[][] messages = [Encoding.UTF8.GetBytes("client"), new byte[64 * 1024], Encoding.UTF8.GetBytes("server")];

        foreach (byte[] message in messages) {
            Assert.Equal(message, client.Decrypt(server.Encrypt(message)));
            Assert.Equal(message, server.Decrypt(client.Encrypt(message)));
        }
    }

    [Fact]
    public void CreateSignsServerHandshakeToken() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using BedrockEncryption encryption = BedrockEncryption.Create(clientKey.ExportParameters(false), out string token);
        string[] parts = token.Split('.');
        Assert.Equal(3, parts.Length);
        byte[] headerBytes = Base64Url.Decode(parts[0]);
        using JsonDocument header = JsonDocument.Parse(headerBytes);
        string encodedKey = header.RootElement.GetProperty("x5u").GetString()!;
        byte[] keyBytes = Convert.FromBase64String(encodedKey);
        using ECDsa serverKey = ECDsa.Create();
        serverKey.ImportSubjectPublicKeyInfo(keyBytes, out int consumed);
        Assert.Equal(keyBytes.Length, consumed);
        byte[] signature = Base64Url.Decode(parts[2]);

        Assert.True(serverKey.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), signature,
            HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    [Fact]
    public void DecryptRejectsModifiedCiphertext() {
        using ECDsa clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using BedrockEncryption encryption = BedrockEncryption.Create(clientKey.ExportParameters(false), out _);
        byte[] ciphertext = encryption.Encrypt(Encoding.UTF8.GetBytes("tamper check"));
        ciphertext[^1] ^= 0x80;

        Assert.Throws<CryptographicException>(() => encryption.Decrypt(ciphertext));
    }

    private static BedrockEncryption CreateClientEncryption(ECParameters clientParameters, string token) {
        string[] parts = token.Split('.');
        using JsonDocument header = JsonDocument.Parse(Base64Url.Decode(parts[0]));
        using JsonDocument payload = JsonDocument.Parse(Base64Url.Decode(parts[1]));
        byte[] serverPublicKey = Convert.FromBase64String(header.RootElement.GetProperty("x5u").GetString()!);
        byte[] salt = Convert.FromBase64String(payload.RootElement.GetProperty("salt").GetString()!);
        using ECDiffieHellman clientAgreement = ECDiffieHellman.Create(clientParameters);
        using ECDiffieHellman serverAgreement = ECDiffieHellman.Create();
        serverAgreement.ImportSubjectPublicKeyInfo(serverPublicKey, out int consumed);
        Assert.Equal(serverPublicKey.Length, consumed);
        byte[] sharedSecret = clientAgreement.DeriveRawSecretAgreement(serverAgreement.PublicKey);
        byte[] keyInput = GC.AllocateUninitializedArray<byte>(salt.Length + sharedSecret.Length);
        salt.CopyTo(keyInput, 0);
        sharedSecret.CopyTo(keyInput, salt.Length);
        byte[] key = SHA256.HashData(keyInput);
        CryptographicOperations.ZeroMemory(sharedSecret);
        CryptographicOperations.ZeroMemory(keyInput);
        return new BedrockEncryption(key);
    }
}
