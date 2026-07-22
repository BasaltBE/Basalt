using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Basalt.Protocol.Login;

internal static class JwtVerification {
    internal static TokenParts ParseTokenParts(string token) {
        int firstDot = token.IndexOf('.');
        int secondDot = firstDot > 0 ? token.IndexOf('.', firstDot + 1) : -1;

        if (firstDot <= 0
            || secondDot <= firstDot + 1
            || secondDot == token.Length - 1
            || token.IndexOf('.', secondDot + 1) >= 0) {
            throw new InvalidOperationException("Malformed JWT.");
        }

        return new TokenParts(0, firstDot, firstDot + 1, secondDot - firstDot - 1, secondDot + 1, token.Length - secondDot - 1);
    }

    internal static string GetAlgorithm(string token) {
        TokenParts parts = ParseTokenParts(token);
        byte[] headerBytes = JsonValue.DecodeBase64Url(token.AsSpan(parts.HeaderStart, parts.HeaderLength));
        using JsonDocument headerDocument = JsonDocument.Parse(headerBytes);
        return JsonValue.GetString(headerDocument.RootElement, "alg");
    }

    internal static bool TryDecodeJwt(string token, out JsonElement header, out JsonElement payload) {
        header = default;
        payload = default;

        try {
            TokenParts parts = ParseTokenParts(token);
            byte[] headerBytes = JsonValue.DecodeBase64Url(token.AsSpan(parts.HeaderStart, parts.HeaderLength));
            byte[] payloadBytes = JsonValue.DecodeBase64Url(token.AsSpan(parts.PayloadStart, parts.PayloadLength));

            JsonDocument headerDocument = JsonDocument.Parse(headerBytes);
            JsonDocument payloadDocument = JsonDocument.Parse(payloadBytes);
            header = headerDocument.RootElement.Clone();
            payload = payloadDocument.RootElement.Clone();
            headerDocument.Dispose();
            payloadDocument.Dispose();
            return true;
        }
        catch {
            return false;
        }
    }

    internal static byte[] GetSigningInput(ReadOnlySpan<char> token, TokenParts parts) {
        byte[] signingInput = ArrayPool<byte>.Shared.Rent(parts.HeaderLength + 1 + parts.PayloadLength);
        int written = Encoding.ASCII.GetBytes(token.Slice(parts.HeaderStart, parts.HeaderLength), signingInput);
        signingInput[written++] = (byte)'.';
        written += Encoding.ASCII.GetBytes(token.Slice(parts.PayloadStart, parts.PayloadLength), signingInput.AsSpan(written));
        return signingInput.AsSpan(0, written).ToArray();
    }

    internal static void VerifyJwtSignature(string token, string identityPublicKey) {
        TokenParts parts = ParseTokenParts(token);
        string algorithm = GetAlgorithm(token);
        byte[] signature = JsonValue.DecodeBase64Url(token.AsSpan(parts.SignatureStart, parts.SignatureLength));
        byte[] signingInput = GetSigningInput(token.AsSpan(), parts);

        if (string.Equals(algorithm, "ES384", StringComparison.OrdinalIgnoreCase)) {
            using ECDsa key = ImportEcdsaPublicKey(identityPublicKey);
            if (!key.VerifyData(signingInput, signature, HashAlgorithmName.SHA384, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) {
                throw new InvalidOperationException("Invalid token signature.");
            }

            return;
        }

        if (string.Equals(algorithm, "ES256", StringComparison.OrdinalIgnoreCase)) {
            using ECDsa key = ImportEcdsaPublicKey(identityPublicKey);
            if (!key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) {
                throw new InvalidOperationException("Invalid token signature.");
            }

            return;
        }

        if (string.Equals(algorithm, "RS256", StringComparison.OrdinalIgnoreCase)) {
            using RSA key = ImportRsaPublicKey(identityPublicKey);
            VerifyRsa256Signature(token.AsSpan(), parts, key, signature);
            return;
        }

        throw new InvalidOperationException($"Unsupported authentication algorithm: {algorithm}.");
    }

    internal static void VerifyRsa256Signature(ReadOnlySpan<char> token, TokenParts parts, RSA key, byte[] signature) {
        byte[] signingInput = ArrayPool<byte>.Shared.Rent(parts.HeaderLength + 1 + parts.PayloadLength);
        try {
            int written = Encoding.ASCII.GetBytes(token.Slice(parts.HeaderStart, parts.HeaderLength), signingInput);
            signingInput[written++] = (byte)'.';
            written += Encoding.ASCII.GetBytes(token.Slice(parts.PayloadStart, parts.PayloadLength), signingInput.AsSpan(written));

            if (!key.VerifyData(signingInput.AsSpan(0, written), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) {
                throw new InvalidOperationException("Invalid token signature.");
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(signingInput, clearArray: true);
        }
    }

    internal static ECDsa ImportEcdsaPublicKey(string identityPublicKey) {
        byte[] keyBytes = Convert.FromBase64String(identityPublicKey);
        ECDsa ecdsa = ECDsa.Create();

        try {
            ecdsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return ecdsa;
        }
        catch (CryptographicException) {
            ecdsa.Dispose();
        }

        if (TryImportRawEcdsaPublicKey(keyBytes, out ECDsa? rawKey) && rawKey is not null) {
            return rawKey;
        }

        throw new InvalidOperationException("Invalid ECDSA public key.");
    }

    internal static RSA ImportRsaPublicKey(string identityPublicKey) {
        byte[] keyBytes = Convert.FromBase64String(identityPublicKey);
        RSA rsa = RSA.Create();

        try {
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return rsa;
        }
        catch (CryptographicException) {
            rsa.Dispose();
        }

        rsa = RSA.Create();
        rsa.ImportRSAPublicKey(keyBytes, out _);
        return rsa;
    }

    private static bool TryImportRawEcdsaPublicKey(ReadOnlySpan<byte> keyBytes, out ECDsa? ecdsa) {
        ecdsa = null;

        if (keyBytes.Length != 96 && keyBytes.Length != 64) {
            return false;
        }

        try {
            ECDsa imported = ECDsa.Create();
            imported.ImportParameters(new ECParameters {
                Curve = keyBytes.Length == 96 ? ECCurve.NamedCurves.nistP384 : ECCurve.NamedCurves.nistP256,
                Q = new ECPoint {
                    X = keyBytes[..(keyBytes.Length / 2)].ToArray(),
                    Y = keyBytes[(keyBytes.Length / 2)..].ToArray()
                }
            });
            ecdsa = imported;
            return true;
        }
        catch (CryptographicException) {
            return false;
        }
    }

    internal readonly record struct TokenParts(
        int HeaderStart,
        int HeaderLength,
        int PayloadStart,
        int PayloadLength,
        int SignatureStart,
        int SignatureLength
    );
}
