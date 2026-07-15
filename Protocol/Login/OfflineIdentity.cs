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

public static class OfflineIdentity
{
    private const string AudienceApi = "api://auth-minecraft-services/multiplayer";

    public static bool IsOfflineLogin(string identityJson)
    {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        return IsOfflineLogin(envelope);
    }

    public static bool IsOfflineLogin(LoginEnvelope envelope)
    {
        if (envelope.AuthenticationType == 1)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.Token))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        if (!JwtVerification.TryDecodeJwt(envelope.Token, out JsonElement header, out JsonElement payload))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string algorithm = JsonValue.GetString(header, "alg");
        if (string.Equals(algorithm, "RS256", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(JsonValue.GetString(payload, "aud"), AudienceApi, StringComparison.Ordinal))
            {
                return false;
            }
        }
        else if (!IsEcAlgorithm(algorithm))
        {
            return false;
        }

        string displayName = JsonValue.GetString(payload, "xname");
        string publicKey = JsonValue.GetString(payload, "cpk");
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(publicKey))
        {
            return TryParseLegacyChain(envelope.Chain, out _);
        }

        string xuid = JsonValue.GetString(payload, "xid");
        if (!string.IsNullOrWhiteSpace(xuid) && IsOnlineIssuer(JsonValue.GetString(payload, "iss")))
        {
            return false;
        }

        return true;
    }

    public static VerifiedIdentity VerifyOffline(LoginEnvelope envelope, string clientJwt)
    {
        if (TryParseLegacyChain(envelope.Chain, out OfflineCertificateData certificate))
        {
            VerifyClientJwt(clientJwt, certificate.IdentityPublicKey);
            string xuid = OfflineIdentity.GetOfflineXuid(certificate.DisplayName);
            return ToVerifiedIdentity(certificate, certificate.DisplayName, xuid);
        }

        if (!string.IsNullOrWhiteSpace(envelope.Token))
        {
            OfflineTokenData token = ParseOfflineToken(envelope.Token);
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

        throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
    }

    public static OfflineCertificateData ParseCertificate(string identityJson)
    {
        LoginEnvelope envelope = LoginEnvelope.Parse(identityJson);
        if (!TryParseLegacyChain(envelope.Chain, out OfflineCertificateData data))
        {
            throw new InvalidOperationException("Invalid offline certificate: missing extraData.");
        }

        return data;
    }

    public static OfflineTokenData ParseOfflineToken(string token)
    {
        if (!JwtVerification.TryDecodeJwt(token, out _, out JsonElement payload))
        {
            throw new InvalidOperationException("Invalid offline token.");
        }

        string displayName = JsonValue.GetString(payload, "xname");
        string publicKey = JsonValue.GetString(payload, "cpk");
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(publicKey))
        {
            throw new InvalidOperationException("Invalid offline token: missing player identity.");
        }

        return new OfflineTokenData(
            publicKey,
            displayName,
            JsonValue.GetString(payload, "identity"),
            JsonValue.GetString(payload, "xid")
        );
    }

    public static void VerifyClientJwt(string clientJwt, string identityPublicKey)
    {
        JwtVerification.VerifyJwtSignature(clientJwt, identityPublicKey);
    }

    public static string GetOfflineXuid(string username)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"OfflineXUID:{username}"));
        ulong value = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(hash);
        return value.ToString().PadLeft(16, '0')[..16];
    }

    public static Guid GetUuidFromUsername(string username)
    {
#pragma warning disable CA5351 
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
#pragma warning restore CA5351
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    public static VerifiedIdentity ToVerifiedIdentity(OfflineCertificateData certificate, string username, string xuid)
    {
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

    private static bool TryParseLegacyChain(string[] chain, out OfflineCertificateData data)
    {
        data = default;

        for (int i = 0; i < chain.Length; i++)
        {
            if (TryParseJwtExtraData(chain[i], out data))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseJwtExtraData(string jwt, out OfflineCertificateData data)
    {
        data = default;

        if (!JwtVerification.TryDecodeJwt(jwt, out _, out JsonElement payload))
        {
            return false;
        }

        if (!payload.TryGetProperty("extraData", out JsonElement extraData))
        {
            return false;
        }

        string displayName = JsonValue.GetString(extraData, "displayName");
        string identity = JsonValue.GetString(extraData, "identity");
        string publicKey = JsonValue.GetString(payload, "identityPublicKey");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        data = new OfflineCertificateData(publicKey, displayName, identity);
        return true;
    }

    private static bool IsEcAlgorithm(string algorithm)
    {
        return string.Equals(algorithm, "ES384", StringComparison.OrdinalIgnoreCase)
            || string.Equals(algorithm, "ES256", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOnlineIssuer(string issuer)
    {
        return issuer.Contains("minecraft-services", StringComparison.OrdinalIgnoreCase)
            || issuer.Contains("mojang", StringComparison.OrdinalIgnoreCase);
    }
}
