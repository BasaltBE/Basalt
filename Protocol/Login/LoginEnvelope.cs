using System.Text.Json;

namespace Basalt.Protocol.Login;

public readonly struct LoginEnvelope {
    public string Token { get; init; }
    public string[] Chain { get; init; }
    public uint AuthenticationType { get; init; }

    public static LoginEnvelope Parse(string identityJson) {
        using JsonDocument document = JsonDocument.Parse(identityJson);
        JsonElement root = document.RootElement;

        string token = GetOptionalString(root, "Token", "token", "AuthorizationToken", "authorizationToken");
        uint authenticationType = 0;
        if (root.TryGetProperty("AuthenticationType", out JsonElement authTypeElement)
            && authTypeElement.TryGetUInt32(out uint parsedAuthType)) {
            authenticationType = parsedAuthType;
        }

        string[] chain = [];
        if (root.TryGetProperty("Certificate", out JsonElement certificateElement)) {
            chain = ParseCertificateElement(certificateElement);
        }
        else if (root.TryGetProperty("certificate", out JsonElement certificateLower)) {
            chain = ParseCertificateElement(certificateLower);
        }

        if (chain.Length == 0 && root.TryGetProperty("chain", out JsonElement rootChain)) {
            chain = ParseChainArray(rootChain);
        }

        if (chain.Length == 0 && root.TryGetProperty("Chain", out JsonElement rootChainUpper)) {
            chain = ParseChainArray(rootChainUpper);
        }

        return new LoginEnvelope {
            Token = token,
            Chain = chain,
            AuthenticationType = authenticationType
        };
    }

    private static string[] ParseCertificateElement(JsonElement certificateElement) {
        if (certificateElement.ValueKind == JsonValueKind.String) {
            string? certificateJson = certificateElement.GetString();
            if (string.IsNullOrWhiteSpace(certificateJson)) {
                return [];
            }

            using JsonDocument certificateDocument = JsonDocument.Parse(certificateJson);
            if (certificateDocument.RootElement.TryGetProperty("chain", out JsonElement chain)) {
                return ParseChainArray(chain);
            }

            return [];
        }

        if (certificateElement.ValueKind == JsonValueKind.Object
            && certificateElement.TryGetProperty("chain", out JsonElement objectChain)) {
            return ParseChainArray(objectChain);
        }

        return [];
    }

    private static string[] ParseChainArray(JsonElement chainElement) {
        if (chainElement.ValueKind != JsonValueKind.Array) {
            return [];
        }

        List<string> chain = [];
        foreach (JsonElement entry in chainElement.EnumerateArray()) {
            if (entry.ValueKind != JsonValueKind.String) {
                continue;
            }

            string? jwt = entry.GetString();
            if (!string.IsNullOrWhiteSpace(jwt)) {
                chain.Add(jwt);
            }
        }

        return chain.ToArray();
    }

    private static string GetOptionalString(JsonElement root, params string[] names) {
        for (int i = 0; i < names.Length; i++) {
            if (root.TryGetProperty(names[i], out JsonElement value)
                && value.ValueKind == JsonValueKind.String) {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
