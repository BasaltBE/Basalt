namespace Basalt.Waterfall.Auth;

public static class AuthEndpoint
{
    // Microsoft Live
    public const string LiveDeviceCode = "https://login.live.com/oauth20_connect.srf";
    public const string LiveToken = "https://login.live.com/oauth20_token.srf";

    // Xbox Live
    public const string XblUserAuth = "https://user.auth.xboxlive.com/user/authenticate";
    public const string XblAuthRelyingParty = "http://auth.xboxlive.com";
    public const string XstsAuthorize = "https://xsts.auth.xboxlive.com/xsts/authorize";

    // Minecraft Bedrock
    public const string BedrockXSTSRelyingParty = "https://multiplayer.minecraft.net/";
    public const string BedrockAuthenticate = "https://multiplayer.minecraft.net/authentication";
    public const string BedrockServicesSessionStart = "https://authorization.franchise.minecraft-services.net/api/v1.0/session/start";
    public const string BedrockMultiplayerSessionStart = "https://authorization.franchise.minecraft-services.net/api/v1.0/multiplayer/session/start";

    // PlayFab
    public const string PlayFabLogin = "https://20ca2.playfabapi.com/Client/LoginWithXbox";
    public const string PlayFabRelyingParty = "https://b980a380.minecraft.playfabapi.com/";
}

public static class XboxErrors
{
    public static readonly Dictionary<long, string> Messages = new()
    {
        { 2148916227, "Your account was banned by Xbox for violating one or more Community Standards for Xbox." },
        { 2148916229, "Your account is restricted. Your guardian has not given you permission to play online." },
        { 2148916230, "This title is not available in your region." },
        { 2148916233, "Your account does not have an Xbox profile. Please create one at https://signup.live.com/signup" },
        { 2148916234, "Your account has not accepted Xbox's Terms of Service." },
        { 2148916235, "Your account resides in a region that Xbox has not authorized use from." },
        { 2148916236, "Your account requires proof of age." },
        { 2148916237, "Your account has reached its limit for playtime." },
        { 2148916238, "The account date of birth is under 18 and cannot proceed unless added to a family by an adult." },
        { 2148916239, "The Xbox Live sandbox is not allowed for this title." },
        { 2148916240, "The request is from a restricted country or region." },
        { 2148916258, "An unknown error occurred while processing the request." }
    };
}
