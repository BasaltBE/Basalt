namespace Basalt.RakNet;

public readonly record struct RaknetServerOptions
{
    public RaknetServerOptions()
    {
        MaxMtu = 1400;
        MaxConnections = 255;
        Advertisement = "MCPE;Basalt;924;1.21.90;0;10;03124212345;Bedrock level;Survival;1;19132;19133;";
        EnableCookies = true;
    }

    public ushort MaxMtu { get; init; }
    public int MaxConnections { get; init; }
    public string Advertisement { get; init; }
    public bool EnableCookies { get; init; }
}
