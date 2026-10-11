namespace BasaltBE.Basalt.Network.Login;

internal readonly record struct AuthenticatedLogin(
    string Username,
    string Xuid,
    Guid Uuid,
    int DeviceOS,
    string DeviceName,
    byte[] Skin
);
