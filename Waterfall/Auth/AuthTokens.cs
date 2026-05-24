namespace Basalt.Waterfall.Auth;

public class MicrosoftTokens
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class XblToken
{
    public string Token { get; set; } = string.Empty;
    public string UserHash { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class XstsToken
{
    public string Token { get; set; } = string.Empty;
    public string UserHash { get; set; } = string.Empty;
    public string Gamertag { get; set; } = string.Empty;
    public string Xuid { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class PlayFabToken
{
    public string SessionTicket { get; set; } = string.Empty;
    public string EntityToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class McServicesToken
{
    public string AuthorizationHeader { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class CachedKeyPair
{
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
}

public class TokenCache
{
    public MicrosoftTokens? Microsoft { get; set; }
    public XblToken? Xbl { get; set; }
    public XstsToken? Xsts { get; set; }
    public PlayFabToken? PlayFab { get; set; }
    public McServicesToken? McServices { get; set; }
    public CachedKeyPair? Keypair { get; set; }
}

public class DeviceCodeResponse
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUri { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}

public class MultiplayerSessionToken
{
    public string SignedToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; }
}

public class UserProfile
{
    public string Xuid { get; set; } = string.Empty;
    public string Uuid { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class AuthResult
{
    public UserProfile Profile { get; set; } = new();
    public XblToken Xbl { get; set; } = new();
    public XstsToken Xsts { get; set; } = new();
    public PlayFabToken PlayFab { get; set; } = new();
    public McServicesToken McServices { get; set; } = new();
    public MultiplayerSessionToken MultiplayerSession { get; set; } = new();
    public List<string> BedrockChain { get; set; } = new();
    public CachedKeyPair Keypair { get; set; } = new();
}
