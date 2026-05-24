using System.Text;
using System.Text.Json;

namespace Basalt.Waterfall.Auth;

/// <summary>
/// Authentication events
/// </summary>
public class AuthEventArgs : EventArgs
{
    public AuthResult? Result { get; set; }
    public DeviceCodeResponse? DeviceCode { get; set; }
    public Exception? Error { get; set; }
}

/// <summary>
/// Main authentication class for Minecraft Bedrock Edition
/// </summary>
public class Authentication
{
    public AuthOptions Options { get; }
    private readonly AuthCache _cache;

    public event EventHandler<AuthEventArgs>? OnLogin;
    public event EventHandler<AuthEventArgs>? OnLogout;
    public event EventHandler<AuthEventArgs>? OnError;
    public event EventHandler<AuthEventArgs>? OnDeviceCode;

    public Authentication(AuthOptions? options = null)
    {
        Options = options ?? new AuthOptions();

        if (Options.Flow == AuthFlow.Password && !string.IsNullOrEmpty(Options.Email) && Options.Username == "default")
        {
            Options.Username = Options.Email.Split('@')[0];
        }

        _cache = new AuthCache(Options.CacheDir, Options.Username);
    }

    public async Task<AuthResult> Login()
    {
        if (string.IsNullOrEmpty(Options.ClientId))
        {
            throw new Exception(
                "Missing clientId. Register an Azure app at https://portal.azure.com and pass the Application (client) ID.");
        }

        var flow = Options.Flow;

        if (flow == AuthFlow.Password)
        {
            if (string.IsNullOrEmpty(Options.Email) || string.IsNullOrEmpty(Options.Password))
            {
                throw new Exception(
                    "Missing email or password. Both are required when using the \"password\" auth flow.");
            }
        }

        if (flow == AuthFlow.XboxToken)
        {
            if (string.IsNullOrEmpty(Options.XboxToken))
            {
                throw new Exception(
                    "Missing xboxToken. Provide the full \"XBL3.0 x={userHash};{token}\" string when using the \"xboxToken\" auth flow.");
            }
            return await LoginWithXboxToken(Options.XboxToken);
        }

        try
        {
            var cached = _cache.Load();
            var keypair = cached.Keypair ?? KeyPairGenerator.GenerateKeyPair();

            var cachedResult = await TryUseCached(cached, keypair);
            if (cachedResult != null)
            {
                OnLogin?.Invoke(this, new AuthEventArgs { Result = cachedResult });
                return cachedResult;
            }

            MicrosoftTokens msTokens;
            if (cached.Microsoft?.RefreshToken != null && !string.IsNullOrEmpty(cached.Microsoft.RefreshToken))
            {
                try
                {
                    msTokens = await MicrosoftAuth.RefreshMicrosoftToken(
                        Options.ClientId,
                        cached.Microsoft.RefreshToken);
                }
                catch
                {
                    msTokens = await ObtainMicrosoftTokens(flow);
                }
            }
            else
            {
                msTokens = await ObtainMicrosoftTokens(flow);
            }

            var result = await AuthenticateChain(msTokens, keypair);

            _cache.Save(new TokenCache
            {
                Microsoft = msTokens,
                Xbl = result.Xbl,
                Xsts = result.Xsts,
                PlayFab = result.PlayFab,
                McServices = result.McServices,
                Keypair = keypair
            });

            OnLogin?.Invoke(this, new AuthEventArgs { Result = result });
            return result;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new AuthEventArgs { Error = ex });
            throw;
        }
    }

    public void Logout()
    {
        var cached = _cache.Load();
        _cache.Save(new TokenCache
        {
            Microsoft = cached.Microsoft,
            Keypair = cached.Keypair
        });
        OnLogout?.Invoke(this, new AuthEventArgs());
    }

    private async Task<AuthResult?> TryUseCached(TokenCache cached, CachedKeyPair keypair)
    {
        if (cached.Xbl != null && AuthCache.IsTokenValid(cached.Xbl.ExpiresAt) &&
            cached.Xsts != null && AuthCache.IsTokenValid(cached.Xsts.ExpiresAt) &&
            cached.PlayFab != null && AuthCache.IsTokenValid(cached.PlayFab.ExpiresAt) &&
            cached.McServices != null && AuthCache.IsTokenValid(cached.McServices.ExpiresAt))
        {
            var clientPublicKey = Options.ClientPublicKey ?? keypair.PublicKey;
            var bedrock = await SessionAuth.GetBedrockChain(cached.Xsts.Token, cached.Xsts.UserHash, clientPublicKey);
            
            var multiplayerSession = new MultiplayerSessionToken();
            if (!string.IsNullOrEmpty(clientPublicKey))
            {
                multiplayerSession = await SessionAuth.GetMultiplayerSessionToken(
                    cached.McServices.AuthorizationHeader,
                    clientPublicKey);
            }
            
            var profile = ExtractProfileFromChains(bedrock);
            
            return new AuthResult
            {
                Profile = profile,
                Xbl = cached.Xbl,
                Xsts = cached.Xsts,
                PlayFab = cached.PlayFab,
                McServices = cached.McServices,
                MultiplayerSession = multiplayerSession,
                BedrockChain = bedrock,
                Keypair = keypair
            };
        }
        return null;
    }

    private async Task<MicrosoftTokens> DoDeviceCodeFlow()
    {
        var deviceCode = await MicrosoftAuth.RequestDeviceCode(Options.ClientId);
        OnDeviceCode?.Invoke(this, new AuthEventArgs { DeviceCode = deviceCode });
        
        return await MicrosoftAuth.PollDeviceCode(
            Options.ClientId,
            deviceCode.DeviceCode,
            deviceCode.Interval,
            deviceCode.ExpiresIn);
    }

    private async Task<MicrosoftTokens> ObtainMicrosoftTokens(AuthFlow flow)
    {
        if (flow == AuthFlow.Password)
        {
            return await MicrosoftAuth.AuthenticateWithPassword(
                Options.ClientId,
                Options.Email!,
                Options.Password!);
        }
        return await DoDeviceCodeFlow();
    }

    private async Task<AuthResult> LoginWithXboxToken(string xboxToken)
    {
        try
        {
            // Parse "XBL3.0 x={userHash};{token}"
            var match = System.Text.RegularExpressions.Regex.Match(
                xboxToken, 
                @"^XBL3\.0\s+x=([^;]+);(.+)$");
            
            if (!match.Success)
            {
                throw new Exception("Invalid xboxToken format. Expected \"XBL3.0 x={userHash};{token}\"");
            }
            
            var userHash = match.Groups[1].Value;
            var token = match.Groups[2].Value;

            var keypair = _cache.Load().Keypair ?? KeyPairGenerator.GenerateKeyPair();
            var clientPublicKey = Options.ClientPublicKey ?? keypair.PublicKey;

            var xsts = new XstsToken
            {
                Token = token,
                UserHash = userHash,
                Gamertag = "",
                Xuid = "",
                ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (24 * 60 * 60 * 1000)
            };

            var xbl = new XblToken
            {
                Token = token,
                UserHash = userHash,
                ExpiresAt = xsts.ExpiresAt
            };

            var bedrock = await SessionAuth.GetBedrockChain(xsts.Token, xsts.UserHash, clientPublicKey);

            var profile = ExtractProfileFromChains(bedrock);
            xsts.Gamertag = profile.Username;
            xsts.Xuid = profile.Xuid;

            var playFab = new PlayFabToken();
            var mcServices = new McServicesToken();
            var multiplayerSession = new MultiplayerSessionToken();

            var result = new AuthResult
            {
                Profile = profile,
                Xbl = xbl,
                Xsts = xsts,
                PlayFab = playFab,
                McServices = mcServices,
                MultiplayerSession = multiplayerSession,
                BedrockChain = bedrock,
                Keypair = keypair
            };

            _cache.Save(new TokenCache
            {
                Xbl = result.Xbl,
                Xsts = result.Xsts,
                PlayFab = result.PlayFab,
                McServices = result.McServices,
                Keypair = keypair
            });

            OnLogin?.Invoke(this, new AuthEventArgs { Result = result });
            return result;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new AuthEventArgs { Error = ex });
            throw;
        }
    }

    private async Task<AuthResult> AuthenticateChain(MicrosoftTokens msTokens, CachedKeyPair keypair)
    {
        var xbl = await XboxAuth.AuthenticateXbl(msTokens.AccessToken);
        var xsts = await XboxAuth.AuthenticateXsts(xbl.Token);

        var xstsPlayFab = await XboxAuth.AuthenticateXsts(xbl.Token, AuthEndpoint.PlayFabRelyingParty);

        if (string.IsNullOrEmpty(xsts.Gamertag) && !string.IsNullOrEmpty(xstsPlayFab.Gamertag))
            xsts.Gamertag = xstsPlayFab.Gamertag;
        if (string.IsNullOrEmpty(xsts.Xuid) && !string.IsNullOrEmpty(xstsPlayFab.Xuid))
            xsts.Xuid = xstsPlayFab.Xuid;

        if (string.IsNullOrEmpty(xsts.Gamertag) || string.IsNullOrEmpty(xsts.Xuid))
        {
            try
            {
                var parts = xsts.Token.Split('.');
                if (parts.Length > 1)
                {
                    var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                    var json = JsonSerializer.Deserialize<JsonElement>(payload);
                    
                    if (json.TryGetProperty("extraData", out var extraData))
                    {
                        if (string.IsNullOrEmpty(xsts.Gamertag) && extraData.TryGetProperty("displayName", out var displayName))
                            xsts.Gamertag = displayName.GetString() ?? "";
                        if (string.IsNullOrEmpty(xsts.Xuid) && extraData.TryGetProperty("XUID", out var xuid))
                            xsts.Xuid = xuid.GetString() ?? "";
                    }
                }
            }
            catch { /* not a JWT or no extraData */ }
        }

        var playFab = await PlayFabAuth.LoginWithPlayFab(xstsPlayFab);
        var mcServices = await SessionAuth.GetMinecraftServicesToken(playFab.SessionTicket);

        var clientPublicKey = Options.ClientPublicKey ?? keypair.PublicKey;
        var bedrock = await SessionAuth.GetBedrockChain(xsts.Token, xsts.UserHash, clientPublicKey);

        var chainProfile = ExtractProfileFromChains(bedrock);
        if (string.IsNullOrEmpty(xsts.Gamertag) && !string.IsNullOrEmpty(chainProfile.Username))
            xsts.Gamertag = chainProfile.Username;
        if (string.IsNullOrEmpty(xsts.Xuid) && !string.IsNullOrEmpty(chainProfile.Xuid))
            xsts.Xuid = chainProfile.Xuid;

        var profile = new UserProfile
        {
            Username = !string.IsNullOrEmpty(xsts.Gamertag) ? xsts.Gamertag : chainProfile.Username,
            Xuid = !string.IsNullOrEmpty(xsts.Xuid) ? xsts.Xuid : chainProfile.Xuid,
            Uuid = chainProfile.Uuid
        };

        var multiplayerSession = new MultiplayerSessionToken();
        if (!string.IsNullOrEmpty(clientPublicKey))
        {
            multiplayerSession = await SessionAuth.GetMultiplayerSessionToken(
                mcServices.AuthorizationHeader,
                clientPublicKey);
        }

        return new AuthResult
        {
            Profile = profile,
            Xbl = xbl,
            Xsts = xsts,
            PlayFab = playFab,
            McServices = mcServices,
            MultiplayerSession = multiplayerSession,
            BedrockChain = bedrock,
            Keypair = keypair
        };
    }

    private static UserProfile ExtractProfileFromChains(List<string> chains)
    {
        foreach (var chain in chains)
        {
            try
            {
                var parts = chain.Split('.');
                if (parts.Length > 1)
                {
                    var base64 = parts[1]
                        .Replace('-', '+')
                        .Replace('_', '/');
                    
                    switch (base64.Length % 4)
                    {
                        case 2: base64 += "=="; break;
                        case 3: base64 += "="; break;
                    }
                    
                    var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    var json = JsonSerializer.Deserialize<JsonElement>(payload);
                    
                    if (json.TryGetProperty("extraData", out var extraData) &&
                        extraData.TryGetProperty("displayName", out var displayName))
                    {
                        return new UserProfile
                        {
                            Username = displayName.GetString() ?? "",
                            Xuid = extraData.TryGetProperty("XUID", out var xuid) ? xuid.GetString() ?? "" : "",
                            Uuid = extraData.TryGetProperty("identity", out var identity) ? identity.GetString() ?? "" : ""
                        };
                    }
                }
            }
            catch { /* skip */ }
        }
        
        return new UserProfile();
    }
}