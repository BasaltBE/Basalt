using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Basalt.Waterfall.Auth;

public static class MicrosoftAuth
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<DeviceCodeResponse> RequestDeviceCode(string clientId)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = "service::user.auth.xboxlive.com::MBI_SSL",
            ["response_type"] = "device_code"
        });

        var response = await _httpClient.PostAsync(AuthEndpoint.LiveDeviceCode, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"Device code request failed: {response.StatusCode} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        return new DeviceCodeResponse
        {
            DeviceCode = data.GetProperty("device_code").GetString() ?? "",
            UserCode = data.GetProperty("user_code").GetString() ?? "",
            VerificationUri = data.TryGetProperty("verification_uri", out var uri) 
                ? uri.GetString() ?? "https://www.microsoft.com/link"
                : "https://www.microsoft.com/link",
            ExpiresIn = data.GetProperty("expires_in").GetInt32(),
            Interval = data.TryGetProperty("interval", out var interval) ? interval.GetInt32() : 5
        };
    }

    public static async Task<MicrosoftTokens> PollDeviceCode(
        string clientId,
        string deviceCode,
        int interval,
        int expiresIn)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(interval * 1000);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["client_id"] = clientId,
                ["device_code"] = deviceCode
            });

            var response = await _httpClient.PostAsync(AuthEndpoint.LiveToken, content);
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (response.IsSuccessStatusCode && data.TryGetProperty("access_token", out var accessToken))
            {
                return new MicrosoftTokens
                {
                    AccessToken = accessToken.GetString() ?? "",
                    RefreshToken = data.GetProperty("refresh_token").GetString() ?? "",
                    ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 
                               (data.GetProperty("expires_in").GetInt64() * 1000)
                };
            }

            if (data.TryGetProperty("error", out var error))
            {
                var errorStr = error.GetString();
                if (errorStr == "authorization_pending") continue;
                if (errorStr == "slow_down")
                {
                    interval += 5;
                    continue;
                }

                var errorDesc = data.TryGetProperty("error_description", out var desc) 
                    ? desc.GetString() : "";
                throw new Exception($"Device code poll failed: {errorStr} - {errorDesc}");
            }
        }

        throw new Exception("Device code flow timed out");
    }

    public static async Task<MicrosoftTokens> RefreshMicrosoftToken(
        string clientId,
        string refreshToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["refresh_token"] = refreshToken,
            ["scope"] = "service::user.auth.xboxlive.com::MBI_SSL"
        });

        var response = await _httpClient.PostAsync(AuthEndpoint.LiveToken, content);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new Exception($"Token refresh failed: {response.StatusCode} - {text}");
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new MicrosoftTokens
        {
            AccessToken = data.GetProperty("access_token").GetString() ?? "",
            RefreshToken = data.GetProperty("refresh_token").GetString() ?? "",
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 
                       (data.GetProperty("expires_in").GetInt64() * 1000)
        };
    }

    public static async Task<MicrosoftTokens> AuthenticateWithPassword(
        string clientId,
        string email,
        string password)
    {
        var authorizeUrl = 
            $"https://login.live.com/oauth20_authorize.srf?client_id={clientId}" +
            "&redirect_uri=https://login.live.com/oauth20_desktop.srf" +
            "&scope=service::user.auth.xboxlive.com::MBI_SSL" +
            "&display=touch&response_type=token&locale=en";

        var pageResponse = await _httpClient.GetAsync(authorizeUrl);
        if (!pageResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to load Microsoft login page: {pageResponse.StatusCode}");
        }
        var pageHtml = await pageResponse.Content.ReadAsStringAsync();

        // Extract PPFT token
        var ppftMatch = Regex.Match(pageHtml, @"sFTTag"":"".*?value=\\""(.+?)\\""|sFTTag':'.*?value=""(.+?)""");
        if (!ppftMatch.Success)
            throw new Exception("Could not extract PPFT token from login page");
        var ppft = ppftMatch.Groups[1].Success ? ppftMatch.Groups[1].Value : ppftMatch.Groups[2].Value;

        // Extract post URL
        var urlPostMatch = Regex.Match(pageHtml, @"urlPost"":""(.+?)""|urlPost:\s*'(.+?)'");
        if (!urlPostMatch.Success)
            throw new Exception("Could not extract post URL from login page");
        var urlPost = (urlPostMatch.Groups[1].Success ? urlPostMatch.Groups[1].Value : urlPostMatch.Groups[2].Value)
            .Replace("\\/", "/");

        var cookies = ExtractCookies(pageResponse);

        var loginContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["login"] = email,
            ["loginfmt"] = email,
            ["passwd"] = password,
            ["PPFT"] = ppft
        });

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, urlPost)
        {
            Content = loginContent
        };
        loginRequest.Headers.Add("Cookie", cookies);

        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler);
        
        var loginResponse = await client.SendAsync(loginRequest);
        var location = loginResponse.Headers.Location?.ToString();
        var allCookies = MergeCookies(cookies, ExtractCookies(loginResponse));
        var lastRedirectBody = "";
        const int maxRedirects = 10;

        for (int i = 0; i < maxRedirects && !string.IsNullOrEmpty(location); i++)
        {
            if (location.Contains("access_token")) break;

            var redirectRequest = new HttpRequestMessage(HttpMethod.Get, location);
            redirectRequest.Headers.Add("Cookie", allCookies);
            
            var redirectResponse = await client.SendAsync(redirectRequest);
            allCookies = MergeCookies(allCookies, ExtractCookies(redirectResponse));

            var nextLocation = redirectResponse.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(nextLocation))
            {
                lastRedirectBody = await redirectResponse.Content.ReadAsStringAsync();
            }
            location = nextLocation ?? location;
        }

        if (string.IsNullOrEmpty(location) || !location.Contains("access_token"))
        {
            var body = lastRedirectBody;
            if (string.IsNullOrEmpty(body))
                body = await loginResponse.Content.ReadAsStringAsync();
            
            throw InferPasswordAuthFailure(location ?? "", body);
        }

        var fragment = location.Split('#')[1];
        var parameters = ParseQueryString(fragment);
        
        var accessToken = parameters.GetValueOrDefault("access_token");
        var refreshToken = parameters.GetValueOrDefault("refresh_token");
        var expiresIn = parameters.GetValueOrDefault("expires_in");

        if (string.IsNullOrEmpty(accessToken))
            throw new Exception("No access_token in redirect response");

        return new MicrosoftTokens
        {
            AccessToken = Uri.UnescapeDataString(accessToken),
            RefreshToken = string.IsNullOrEmpty(refreshToken) ? "" : Uri.UnescapeDataString(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 
                       (long.Parse(expiresIn ?? "86400") * 1000)
        };
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
        }
        return result;
    }

    private static string ExtractCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            return "";
        
        return string.Join("; ", setCookies.Select(c => c.Split(';')[0]));
    }

    private static string MergeCookies(string existing, string incoming)
    {
        if (string.IsNullOrEmpty(incoming)) return existing;
        if (string.IsNullOrEmpty(existing)) return incoming;
        return $"{existing}; {incoming}";
    }

    private static Exception InferPasswordAuthFailure(string lastUrl, string body)
    {
        // Identity confirmation / account protection
        if (lastUrl.Contains("identity/confirm") ||
            body.Contains("identity/confirm") ||
            body.Contains("Help us protect your account") ||
            body.Contains("account.live.com/identity"))
        {
            return new Exception(
                "Microsoft requires identity verification for this account. " +
                "Complete the verification at https://login.live.com in a browser, or use the device code flow instead.");
        }

        // Two-factor authentication
        if (lastUrl.Contains("LiveTwoStepVerification") ||
            body.Contains("two-step verification") ||
            body.Contains("2FA"))
        {
            return new Exception(
                "Two-factor authentication is enabled on this account. Use the device code flow instead.");
        }

        // Stay signed in prompt
        if (body.Contains("Stay signed in?") ||
            body.Contains("Keep me signed in") ||
            body.Contains("kmsi") ||
            lastUrl.Contains("kmsi"))
        {
            return new Exception("Microsoft stopped on a 'Stay signed in?' confirmation step.");
        }

        // Permissions/consent
        if (body.Contains("Permissions requested") ||
            body.Contains("Review permissions") ||
            body.Contains("Let this app access your info") ||
            lastUrl.Contains("consent"))
        {
            return new Exception("Microsoft stopped on an app consent/permissions step.");
        }

        // Bad credentials
        if (body.Contains("password is incorrect") ||
            body.Contains("Your account or password is incorrect") ||
            body.Contains("That Microsoft account doesn") ||
            body.Contains("Enter the password for") ||
            body.Contains("Try again, or use a different password"))
        {
            return new Exception("Invalid email or password");
        }

        return new Exception(
            "Email/password authentication failed: Microsoft returned an unrecognized login step. " +
            "This often means the account needs an interactive sign-in, protection check, or a changed Microsoft login page.");
    }
}
