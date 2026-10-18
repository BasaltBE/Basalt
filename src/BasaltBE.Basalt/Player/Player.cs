using System.Buffers.Binary;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class Player : Entity, IDisposable
{
    public readonly Lock Sync = new();
    internal readonly Lock TickSync = new();
    internal readonly NetworkServer Network;
    public readonly PlayerAbilities Abilities = new();
    public readonly PlayerPermissions Permissions;

    public readonly World World;
    public readonly Dimension Dimension;

    /// <summary>NetherNet connection associated with this player.</summary>
    public readonly NethernetPeer Peer;

    /// <summary>Username from the Bedrock login data.</summary>
    public readonly string Username;

    /// <summary>Xbox user ID from the Bedrock login data.</summary>
    public readonly string Xuid;

    /// <summary>Player UUID from the Bedrock login data.</summary>
    public readonly Guid Uuid;

    /// <summary>Device operating system identifier from the Bedrock login data.</summary>
    public readonly int DeviceOS;

    /// <summary>Device name from the Bedrock login data.</summary>
    public readonly string DeviceName;

    /// <summary>Serialized skin data from the Bedrock login data.</summary>
    public readonly byte[] Skin;
    internal readonly PlayerListEntry ListEntry;
    internal bool Listed;

    public int ChunkRadius = 16;
    public bool Spawned;
    internal BlockPos? BreakingBlock;
    internal ulong BlockBreakStartTick;
    internal uint BlockBreakDurationTicks;
    internal Container? OpenedContainer;
    internal int PendingCommands;

    /// <summary>A list of resource packs requested by the player. </summary>
    internal readonly HashSet<Guid> RequestedResourcePacks = [];

    public bool Disposed;

    public Player(
        NetworkServer network,
        World world,
        Dimension dimension,
        NethernetPeer peer,
        string username,
        string xuid,
        Guid uuid,
        int deviceOS,
        string deviceName,
        byte[] skin
    ) : base(EntityPalette.Find("minecraft:player"), 1.62f)
    {
        Network = network;
        World = world;
        Dimension = dimension;
        Peer = peer;
        Username = username;
        Xuid = xuid;
        Permissions = network.Server.Permissions.Get(xuid, username);
        Uuid = uuid;
        DeviceOS = deviceOS;
        DeviceName = deviceName;
        Skin = skin;
        Span<byte> uuidBytes = stackalloc byte[16];
        uuid.TryWriteBytes(uuidBytes, bigEndian: true, out _);
        ListEntry = new PlayerListEntry
        {
            Action = PlayerListAction.Add,
            AddEntry = new PlayerListAddEntry
            {
                Uuid = new Uuid
                {
                    MostSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(uuidBytes),
                    LeastSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(uuidBytes[8..])
                },
                ActorId = UniqueId,
                Name = username,
                Xuid = xuid,
                BuildPlatform = (BuildPlatform)deviceOS,
                SkinBytes = skin
            }
        };
        Position.X = 0.5f;
        Position.Y = -57f;
        Position.Z = 0.5f;

        Attributes.Set("minecraft:movement", 0, float.MaxValue, 0.1f, 0, float.MaxValue, 0.1f);
        Attributes.Set("minecraft:underwater_movement", 0, float.MaxValue, 0.02f, 0, float.MaxValue, 0.02f);
        Attributes.Set("minecraft:lava_movement", 0, float.MaxValue, 0.02f, 0, float.MaxValue, 0.02f);
        SetFlag(ActorFlag.HasGravity, true);
        SetFlag(ActorFlag.Breathing, true);
        SetFlag(ActorFlag.CanShowName, true);
        SetFlag(ActorFlag.AlwaysShowName, true);
        SetFlag(ActorFlag.CanClimb, true);
    }

    public void Dispose()
    {
        lock (Sync)
        {
            if (Disposed) return;
            Disposed = true;
        }
    }
}
