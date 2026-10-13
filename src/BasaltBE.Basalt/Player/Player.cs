using BasaltBE.Basalt.Entities.Types;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class Player(
    NethernetPeer peer,
    string username,
    string xuid,
    Guid uuid,
    int deviceOS,
    string deviceName,
    byte[] skin
) : Entity(EntityPalette.Find("minecraft:player")), IDisposable {
    public readonly Lock Sync = new();

    /// <summary>NetherNet connection associated with this player.</summary>
    public readonly NethernetPeer Peer = peer;

    /// <summary>Username from the Bedrock login data.</summary>
    public readonly string Username = username;

    /// <summary>Xbox user ID from the Bedrock login data.</summary>
    public readonly string Xuid = xuid;

    /// <summary>Player UUID from the Bedrock login data.</summary>
    public readonly Guid Uuid = uuid;

    /// <summary>Device operating system identifier from the Bedrock login data.</summary>
    public readonly int DeviceOS = deviceOS;

    /// <summary>Device name from the Bedrock login data.</summary>
    public readonly string DeviceName = deviceName;

    /// <summary>Serialized skin data from the Bedrock login data.</summary>
    public readonly byte[] Skin = skin;

    public int ChunkRadius;
    public bool Spawned;

    /// <summary>A list of resource packs requested by the player. </summary>
    internal readonly HashSet<Guid> RequestedResourcePacks = [];

    public bool Disposed;

    public void Dispose() {
        lock (Sync) {
            if (Disposed) return;
            Disposed = true;
        }
    }
}
