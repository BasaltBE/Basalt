using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Network.Login;

internal static class LoginTokenParser {
    internal static LoginTokenData Parse(ReadOnlyMemory<byte> connectionRequest) {
        using ProfilerZone zone = Profiler.BeginZone("Login.ParseTokens");
        int offset = 0;
        ReadOnlyMemory<byte> identityBytes = ReadToken(connectionRequest, ref offset);
        ReadOnlyMemory<byte> clientToken = ReadToken(connectionRequest, ref offset);
        if (offset != connectionRequest.Length)
            throw new InvalidDataException("Login connection request has trailing token data.");
        LoginIdentity identity = JsonSerializer.Deserialize<LoginIdentity>(identityBytes.Span, LoginJson.Options);
        return new LoginTokenData(identity, ParseClientToken(clientToken));
    }

    internal static ClientTokenData ParseClientToken(ReadOnlyMemory<byte> clientToken) {
        using ProfilerZone zone = Profiler.BeginZone("Login.ValidateClientToken");
        ReadOnlySpan<byte> token = clientToken.Span;
        int firstDot = token.IndexOf((byte)'.');
        int secondDotOffset = firstDot < 0 ? -1 : token[(firstDot + 1)..].IndexOf((byte)'.');
        int secondDot = secondDotOffset < 0 ? -1 : firstDot + secondDotOffset + 1;
        if (firstDot <= 0 || secondDot <= firstDot + 1 || secondDot >= token.Length - 1)
            throw new InvalidDataException("Bedrock client token is invalid.");

        ClientTokenHeader header = JsonSerializer.Deserialize<ClientTokenHeader>(
            Base64Url.Decode(token[..firstDot]), LoginJson.Options);
        if (header.Algorithm != "ES384" || string.IsNullOrEmpty(header.PublicKey))
            throw new CryptographicException("Bedrock client token uses an unsupported key.");

        byte[] encodedKey = Base64Url.Decode(header.PublicKey.AsSpan());
        using ECDsa clientKey = ECDsa.Create();
        clientKey.ImportSubjectPublicKeyInfo(encodedKey, out int bytesRead);
        if (bytesRead != encodedKey.Length || clientKey.KeySize != 384)
            throw new CryptographicException("Bedrock client public key must use P-384.");

        byte[] signature = Base64Url.Decode(token[(secondDot + 1)..]);
        if (!clientKey.VerifyData(token[..secondDot], signature, HashAlgorithmName.SHA384,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
            throw new CryptographicException("Bedrock client token signature is invalid.");

        ReadOnlySpan<byte> encodedClientData = token[(firstDot + 1)..secondDot];
        byte[] clientDataJson = ArrayPool<byte>.Shared.Rent(encodedClientData.Length);
        try {
            int clientDataLength = Base64Url.Decode(encodedClientData, clientDataJson);
            ClientData client = JsonSerializer.Deserialize<ClientData>(
                clientDataJson.AsSpan(0, clientDataLength), LoginJson.Options)
                ?? throw new InvalidDataException("Client data is missing.");
            return new ClientTokenData(client, clientKey.ExportParameters(false));
        }
        finally {
            ArrayPool<byte>.Shared.Return(clientDataJson, clearArray: true);
        }
    }

    private static ReadOnlyMemory<byte> ReadToken(ReadOnlyMemory<byte> request, ref int offset) {
        if (request.Length - offset < sizeof(int))
            throw new InvalidDataException("Login token length is incomplete.");
        int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(request.Span[offset..]);
        offset += sizeof(int);
        if (length <= 0 || length > 4 * 1024 * 1024 || length > request.Length - offset)
            throw new InvalidDataException("Login token length is invalid.");
        ReadOnlyMemory<byte> value = request.Slice(offset, length);
        offset += length;
        return value;
    }
}
