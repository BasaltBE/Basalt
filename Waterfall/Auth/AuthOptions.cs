namespace Basalt.Waterfall.Auth;

/// <summary>
/// Authentication flow types
/// </summary>
public enum AuthFlow
{
    DeviceCode,
    Password,
    XboxToken
}

/// <summary>
/// Configuration options for authentication
/// </summary>
public class AuthOptions
{
    /// <summary>
    /// Azure application client ID
    /// </summary>
    public string ClientId { get; set; } = "000000004C12AE6F";

    /// <summary>
    /// Authentication flow to use
    /// </summary>
    public AuthFlow Flow { get; set; } = AuthFlow.DeviceCode;

    /// <summary>
    /// Username for cache identification
    /// </summary>
    public string Username { get; set; } = "default";

    /// <summary>
    /// Directory to store authentication cache
    /// </summary>
    public string CacheDir { get; set; } = ".waterfall/auth";

    /// <summary>
    /// Client public key for encryption (optional)
    /// </summary>
    public string? ClientPublicKey { get; set; }

    /// <summary>
    /// Email address (required for password flow)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Password (required for password flow)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Xbox token in format "XBL3.0 x={userHash};{token}" (required for xboxToken flow)
    /// </summary>
    public string? XboxToken { get; set; }
}
