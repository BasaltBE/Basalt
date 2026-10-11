using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Network.Login;

internal static class LoginJson {
    internal static readonly JsonSerializerOptions Options = new() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };
}
