using System.Security.Cryptography;

namespace BasaltBE.Basalt.Network.Login;

internal readonly record struct ClientTokenData(
    ClientData Client,
    ECParameters PublicKey
);
