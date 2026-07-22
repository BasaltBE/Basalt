using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Basalt.Protocol.Login;

public readonly record struct OfflineCertificateData(
    string IdentityPublicKey,
    string DisplayName,
    string IdentityUuid
);

public readonly record struct OfflineTokenData(
    string IdentityPublicKey,
    string DisplayName,
    string IdentityUuid,
    string Xuid
);

public static class OfflineIdentity {
    private const string AudienceApi = "api://auth-minecraft-services/multiplayer";

    // Bedrock AuthenticationType: Online = 0, SubClient = 1, OfflineSelfSigned = 2
    private const uint AuthOnline = 0;
    private const uint AuthSubClient = 1;
    private const uint AuthOfflineSelfSigned = 2;

    public static bool IsOfflineLogin(string identityJson) {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        return IsOfflineLogin(envelope);
    }

    public static bool IsOfflineLogin(LoginEnvelope envelope) {
        // OfflineSelfSigned (type 2) is always the self-signed/offline path.
        if (envelope.AuthenticationType == AuthOfflineSelfSigned) {
            return true;
        }

        if (envelope.AuthenticationType == AuthSubClient) {
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.Token)) {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        if (!JwtVerification.TryDecodeJwt(envelope.Token, out JsonElement header, out JsonElement payload)) {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string algorithm = JsonValue.GetString(header, "alg");
        if (IsEcAlgorithm(algorithm)) {
            return true;
        }

        if (string.Equals(algorithm, "RS256", StringComparison.OrdinalIgnoreCase)
            && string.Equals(JsonValue.GetString(payload, "aud"), AudienceApi, StringComparison.Ordinal)) {
            return false;
        }

        string displayName = ResolveDisplayName(payload);
        string publicKey = ResolvePublicKey(header, payload);
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(publicKey)) {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string xuid = JsonValue.GetString(payload, "xid");
        if (envelope.AuthenticationType == AuthOnline
            || (!string.IsNullOrWhiteSpace(xuid) && IsOnlineIssuer(JsonValue.GetString(payload, "iss")))) {
            return false;
        }

        return true;
    }

    public static VerifiedIdentity VerifyOffline(LoginEnvelope envelope, string clientJwt) {
        if (envelope.AuthenticationType == AuthOfflineSelfSigned) {
            if (!string.IsNullOrWhiteSpace(envelope.Token)
                && TryBuildOfflineToken(envelope.Token, clientJwt, out OfflineTokenData token, out _)) {
                return CompleteOfflineToken(envelope.Token, clientJwt, token);
            }

            if (TryParseLegacyChain(envelope.Chain, out OfflineCertificateData selfSignedCertificate)) {
                return CompleteLegacyCertificate(selfSignedCertificate, clientJwt);
            }

            if (!string.IsNullOrWhiteSpace(envelope.Token)) {
                _ = ParseOfflineToken(envelope.Token, clientJwt);
            }

            throw new InvalidOperationException("Invalid offline certificate: missing player identity.");
        }

        if (TryParseLegacyChain(envelope.Chain, out OfflineCertificateData certificate)) {
            return CompleteLegacyCertificate(certificate, clientJwt);
        }

        if (!string.IsNullOrWhiteSpace(envelope.Token)) {
            return CompleteOfflineToken(envelope.Token, clientJwt, ParseOfflineToken(envelope.Token, clientJwt));
        }

        throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
    }

    private static VerifiedIdentity CompleteLegacyCertificate(OfflineCertificateData certificate, string clientJwt) {
        string publicKey = certificate.IdentityPublicKey;
        if (string.IsNullOrWhiteSpace(publicKey)
            && JwtVerification.TryDecodeJwt(clientJwt, out JsonElement clientHeader, out _)) {
            publicKey = GetStringIgnoreCase(clientHeader, "x5u");
        }

        if (string.IsNullOrWhiteSpace(publicKey)) {
            throw new InvalidOperationException("Invalid offline certificate: missing public key.");
        }

        VerifyClientJwt(clientJwt, publicKey);
        string xuid = GetOfflineXuid(certificate.DisplayName);
        return ToVerifiedIdentity(
            new OfflineCertificateData(publicKey, certificate.DisplayName, certificate.IdentityUuid),
            certificate.DisplayName,
            xuid);
    }

    private static VerifiedIdentity CompleteOfflineToken(string identityJwt, string clientJwt, OfflineTokenData token) {
        VerifyIdentityJwt(identityJwt, token.IdentityPublicKey);
        VerifyClientJwt(clientJwt, token.IdentityPublicKey);

        string xuid = string.IsNullOrWhiteSpace(token.Xuid)
            ? GetOfflineXuid(token.DisplayName)
            : token.Xuid;

        Guid uuid = Guid.TryParse(token.IdentityUuid, out Guid parsed)
            ? parsed
            : GetUuidFromUsername(token.DisplayName);

        return new VerifiedIdentity(
            token.IdentityPublicKey,
            token.DisplayName,
            xuid,
            uuid.ToString()
        );
    }

    public static OfflineCertificateData ParseCertificate(string identityJson) {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        if (!TryParseLegacyChain(envelope.Chain, out OfflineCertificateData data)) {
            throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
        }

        return data;
    }

    public static OfflineTokenData ParseOfflineToken(string token) =>
        ParseOfflineToken(token, clientJwt: null);

    public static OfflineTokenData ParseOfflineToken(string token, string? clientJwt) {
        if (!TryBuildOfflineToken(token, clientJwt, out OfflineTokenData data, out string error)) {
            throw new InvalidOperationException(error);
        }

        return data;
    }

    private static bool TryBuildOfflineToken(
        string token,
        string? clientJwt,
        out OfflineTokenData data,
        out string error) {
        data = default;
        error = string.Empty;

        if (!JwtVerification.TryDecodeJwt(token, out JsonElement header, out JsonElement payload)) {
            error = "Invalid offline token.";
            return false;
        }

        string displayName = ResolveDisplayName(payload);
        string publicKey = ResolvePublicKey(header, payload);

        JsonElement clientHeader = default;
        JsonElement clientPayload = default;
        bool hasClient = !string.IsNullOrWhiteSpace(clientJwt)
            && JwtVerification.TryDecodeJwt(clientJwt, out clientHeader, out clientPayload);

        if (string.IsNullOrWhiteSpace(publicKey) && hasClient) {
            publicKey = GetStringIgnoreCase(clientHeader, "x5u");
        }

        if (string.IsNullOrWhiteSpace(displayName) && hasClient) {
            displayName = GetStringIgnoreCase(clientPayload, "ThirdPartyName");
        }

        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(publicKey)) {
            string headerKeys = DescribeKeys(header);
            string claimKeys = DescribeKeys(payload);
            error =
                $"Invalid offline token: missing player identity (nameEmpty={string.IsNullOrWhiteSpace(displayName)}, keyEmpty={string.IsNullOrWhiteSpace(publicKey)}, header=[{headerKeys}], claims=[{claimKeys}]).";
            return false;
        }

        data = new OfflineTokenData(
            publicKey,
            displayName,
            ResolveIdentityUuid(payload),
            GetStringIgnoreCase(payload, "xid")
        );
        return true;
    }

    public static void VerifyClientJwt(string clientJwt, string identityPublicKey) {
        JwtVerification.VerifyJwtSignature(clientJwt, identityPublicKey);
    }

    public static void VerifyIdentityJwt(string identityJwt, string identityPublicKey) {
        JwtVerification.VerifyJwtSignature(identityJwt, identityPublicKey);
    }

    public static string GetOfflineXuid(string username) {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"OfflineXUID:{username}"));
        ulong value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(hash);
        return value.ToString().PadLeft(16, '0')[..16];
    }

    public static Guid GetUuidFromUsername(string username) {
#pragma warning disable CA5351 
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
#pragma warning restore CA5351
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    public static VerifiedIdentity ToVerifiedIdentity(OfflineCertificateData certificate, string username, string xuid) {
        Guid uuid = Guid.TryParse(certificate.IdentityUuid, out Guid parsed)
            ? parsed
            : GetUuidFromUsername(username);

        return new VerifiedIdentity(
            certificate.IdentityPublicKey,
            username,
            xuid,
            uuid.ToString()
        );
    }

    private static string ResolveDisplayName(JsonElement payload) {
        string displayName = GetStringIgnoreCase(payload, "xname", "displayName", "ThirdPartyName");
        if (!string.IsNullOrWhiteSpace(displayName)) {
            return displayName;
        }

        if (TryGetPropertyIgnoreCase(payload, "extraData", out JsonElement extraData)) {
            return GetStringIgnoreCase(extraData, "displayName", "xname", "ThirdPartyName");
        }

        return string.Empty;
    }

    private static string ResolvePublicKey(JsonElement header, JsonElement payload) {
        string publicKey = GetStringIgnoreCase(payload, "cpk", "identityPublicKey", "ClientPublicKey");
        if (!string.IsNullOrWhiteSpace(publicKey)) {
            return publicKey;
        }

        return GetStringIgnoreCase(header, "x5u");
    }

    private static string ResolveIdentityUuid(JsonElement payload) {
        string identity = GetStringIgnoreCase(payload, "identity", "leguuid", "uuid", "sub");
        if (!string.IsNullOrWhiteSpace(identity)) {
            return identity;
        }

        if (TryGetPropertyIgnoreCase(payload, "extraData", out JsonElement extraData)) {
            return GetStringIgnoreCase(extraData, "identity", "leguuid", "uuid");
        }

        return string.Empty;
    }

    private static string GetStringIgnoreCase(JsonElement element, params string[] names) {
        foreach (string name in names) {
            if (TryGetPropertyIgnoreCase(element, name, out JsonElement value)
                && value.ValueKind == JsonValueKind.String) {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value) {
        if (element.ValueKind != JsonValueKind.Object) {
            value = default;
            return false;
        }

        foreach (JsonProperty property in element.EnumerateObject()) {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string DescribeKeys(JsonElement element) {
        if (element.ValueKind != JsonValueKind.Object) {
            return element.ValueKind.ToString();
        }

        return string.Join(',', element.EnumerateObject().Select(property => property.Name));
    }

    private static bool TryParseLegacyChain(string[] chain, out OfflineCertificateData data) {
        data = default;

        for (int i = 0; i < chain.Length; i++) {
            if (TryParseJwtExtraData(chain[i], out data)) {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseJwtExtraData(string jwt, out OfflineCertificateData data) {
        data = default;

        if (!JwtVerification.TryDecodeJwt(jwt, out JsonElement header, out JsonElement payload)) {
            return false;
        }

        if (!TryGetPropertyIgnoreCase(payload, "extraData", out JsonElement extraData)) {
            return false;
        }

        string displayName = GetStringIgnoreCase(extraData, "displayName", "xname");
        string identity = GetStringIgnoreCase(extraData, "identity", "leguuid");
        string publicKey = ResolvePublicKey(header, payload);

        if (string.IsNullOrWhiteSpace(displayName)) {
            return false;
        }

        data = new OfflineCertificateData(publicKey, displayName, identity);
        return true;
    }

    private static bool IsEcAlgorithm(string algorithm) {
        return string.Equals(algorithm, "ES384", StringComparison.OrdinalIgnoreCase)
            || string.Equals(algorithm, "ES256", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnlineIssuer(string issuer) {
        return issuer.Contains("minecraft-services", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("mojang", StringComparison.OrdinalIgnoreCase);
    }
}
