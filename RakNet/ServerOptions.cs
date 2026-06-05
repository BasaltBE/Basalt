namespace Basalt.RakNet;

public readonly record struct RaknetServerOptions(
    ushort MaxMtu = 1400,
    int MaxConnections = 255,
    string Advertisement = "MCPE;Basalt;924;1.21.90;0;10;03124212345;Bedrock level;Survival;1;19132;19133;",
    bool EnableCookies = true,
    ushort Port = 19132
)
{
    public const string DefaultAdvertisement = "MCPE;Basalt;924;1.21.90;0;10;03124212345;Bedrock level;Survival;1;19132;19133;";
}
