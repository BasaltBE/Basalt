namespace BasaltBE.Basalt.Network.Login;

internal readonly record struct LoginTokenData(LoginIdentity Identity, ClientTokenData Client);
