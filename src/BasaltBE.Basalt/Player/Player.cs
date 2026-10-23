using System.Buffers.Binary;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Network;

using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.NetherNet.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class Player : Entity, IDisposable {
    public readonly Lock Sync = new();
    internal readonly Lock TickSync = new();
    internal readonly NetworkServer Network;
    public readonly PlayerAbilities Abilities = new();
    public readonly PlayerPermissions Permissions;

    public World World;
    public Dimension Dimension;

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
    internal InputMode InputMode;
    internal readonly PlayerListEntry ListEntry;
    internal bool Listed;

    public int ChunkRadius = 16;
    public bool Spawned;
    public GameType GameType = GameType.Survival;
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
    ) : base(EntityPalette.Find("minecraft:player"), 1.62f) {
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
        ListEntry = new PlayerListEntry {
            Action = PlayerListAction.Add,
            AddEntry = new PlayerListAddEntry {
                Uuid = new Uuid {
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
        Position.X = dimension.SpawnPosition.X;
        Position.Y = dimension.SpawnPosition.Y;
        Position.Z = dimension.SpawnPosition.Z;

        Attributes.Set("minecraft:movement", 0, float.MaxValue, 0.1f, 0, float.MaxValue, 0.1f);
        Attributes.Set("minecraft:underwater_movement", 0, float.MaxValue, 0.02f, 0, float.MaxValue, 0.02f);
        Attributes.Set("minecraft:lava_movement", 0, float.MaxValue, 0.02f, 0, float.MaxValue, 0.02f);
        SetFlag(ActorFlag.HasGravity, true);
        SetFlag(ActorFlag.Breathing, true);
        SetFlag(ActorFlag.CanShowName, true);
        SetFlag(ActorFlag.AlwaysShowName, true);
        SetFlag(ActorFlag.CanClimb, true);
    }

    public bool SetGameType(GameType gameType) {
        if (gameType is not (GameType.Survival or GameType.Creative or GameType.Adventure or GameType.Spectator))
            throw new ArgumentOutOfRangeException(nameof(gameType));
        if (GameType == gameType)
            return false;

        GameType = gameType;
        bool canBuild = gameType is GameType.Survival or GameType.Creative;
        bool spectator = gameType == GameType.Spectator;
        Abilities.Set(PlayerAbility.Build, canBuild);
        Abilities.Set(PlayerAbility.Mine, canBuild);
        Abilities.Set(PlayerAbility.MayFly, gameType is GameType.Creative or GameType.Spectator);
        Abilities.Set(PlayerAbility.InstantBuild, gameType == GameType.Creative);
        Abilities.Set(PlayerAbility.Invulnerable, gameType is GameType.Creative or GameType.Spectator);
        Abilities.Set(PlayerAbility.Flying, spectator);
        Abilities.Set(PlayerAbility.NoClip, spectator);

        if (Spawned) {
            Network.SendPacket(this, new SetPlayerGameTypePacket { GameType = gameType });
            Network.SendPacket(this, Abilities.CreateUpdate(UniqueId, Permissions));
            Dimension.Broadcast(new UpdatePlayerGameTypePacket {
                GameType = gameType,
                TargetPlayerUniqueId = UniqueId,
                Tick = GetTrait<EntityMovementTrait>()?.InputTick ?? 0
            }, this);
        }
        return true;
    }

    internal void LoadData(CompoundTag data, bool restoredLocation) {
        Read(data);
        if (data.Get<IntTag>("GameType") is { } gameType && Enum.IsDefined((GameType)gameType.Value))
            SetGameType((GameType)gameType.Value);
        if (!restoredLocation)
            return;

        Position.X = data.Get<FloatTag>("PosX")?.Value ?? Position.X;
        Position.Y = data.Get<FloatTag>("PosY")?.Value ?? Position.Y;
        Position.Z = data.Get<FloatTag>("PosZ")?.Value ?? Position.Z;
        Rotation.X = data.Get<FloatTag>("RotationX")?.Value ?? Rotation.X;
        Rotation.Y = data.Get<FloatTag>("RotationY")?.Value ?? Rotation.Y;
        HeadRotation = data.Get<FloatTag>("HeadRotation")?.Value ?? HeadRotation;
        if (!float.IsFinite(Position.X) || !float.IsFinite(Position.Y) || !float.IsFinite(Position.Z) ||
            !float.IsFinite(Rotation.X) || !float.IsFinite(Rotation.Y) || !float.IsFinite(HeadRotation)) {
            Position.X = Dimension.SpawnPosition.X;
            Position.Y = Dimension.SpawnPosition.Y;
            Position.Z = Dimension.SpawnPosition.Z;
            Rotation.X = 0;
            Rotation.Y = 0;
            HeadRotation = 0;
        }
    }

    internal CompoundTag SaveData() {
        CompoundTag data = new() { Name = "Player" };
        data.Add("World", new StringTag { Value = World.Identifier });
        data.Add("Dimension", new StringTag { Value = Dimension.Identifier });
        data.Add("PosX", new FloatTag { Value = Position.X });
        data.Add("PosY", new FloatTag { Value = Position.Y });
        data.Add("PosZ", new FloatTag { Value = Position.Z });
        data.Add("RotationX", new FloatTag { Value = Rotation.X });
        data.Add("RotationY", new FloatTag { Value = Rotation.Y });
        data.Add("HeadRotation", new FloatTag { Value = HeadRotation });
        data.Add("GameType", new IntTag { Value = (int)GameType });
        Write(data);
        return data;
    }

    public void TransferTo(Dimension dimension) {
        ArgumentNullException.ThrowIfNull(dimension);
        using Lock.Scope tickSync = TickSync.EnterScope();
        if (Disposed || !Spawned)
            throw new InvalidOperationException("Only a spawned player can transfer dimensions.");

        World sourceWorld = World;
        Dimension sourceDimension = Dimension;
        World targetWorld = dimension.World
            ?? throw new InvalidOperationException("The target dimension does not belong to a world.");
        if (!ReferenceEquals(targetWorld.Server, Network.Server))
            throw new ArgumentException("The target dimension belongs to another server.", nameof(dimension));
        if (dimension.Type == DimensionType.Undefined)
            throw new ArgumentException("The target dimension has no supported Bedrock dimension type.", nameof(dimension));
        if (ReferenceEquals(sourceDimension, dimension))
            return;

        sourceWorld.CheckOwnership();
        targetWorld.CheckOwnership();
        if (!ReferenceEquals(targetWorld, sourceWorld) &&
            (targetWorld.GetPlayerByXuid(Xuid) is not null || targetWorld.GetPlayer(Username) is not null))
            throw new InvalidOperationException("The target world already contains this player's identity.");

        OpenedContainer?.Close(this);
        GetTrait<PlayerChunkRenderingTrait>()?.UnloadChunks();
        OnDespawn();
        sourceWorld.RemovePlayer(this);

        World = targetWorld;
        Dimension = dimension;
        Position.X = dimension.SpawnPosition.X;
        Position.Y = dimension.SpawnPosition.Y;
        Position.Z = dimension.SpawnPosition.Z;
        Rotation.X = 0;
        Rotation.Y = 0;
        HeadRotation = 0;
        SetFlag(ActorFlag.Moving, false);
        SetFlag(ActorFlag.Sneaking, false);
        SetFlag(ActorFlag.Sprinting, false);
        SetFlag(ActorFlag.Swimming, false);
        SetFlag(ActorFlag.Gliding, false);
        SetFlag(ActorFlag.Crawling, false);
        if (GetTrait<EntityMovementTrait>() is { } movement) {
            movement.Motion.X = 0;
            movement.Motion.Y = 0;
            movement.Motion.Z = 0;
            movement.Jumping = false;
            movement.HorizontalCollision = false;
            movement.VerticalCollision = false;
            movement.Grounded = false;
        }

        targetWorld.AddPlayer(this);
        OnSpawn();
        OnMove();

        if (sourceDimension.Type != dimension.Type) {
            Network.SendPacket(this, new ChangeDimensionPacket {
                Dimension = dimension.Type,
                Position = GetHeadPosition(),
                Respawn = true,
                LoadingScreenId = 0
            });
        }
        Network.SendPacket(this, new MovePlayerPacket {
            PlayerRuntimeId = RuntimeId,
            Position = GetHeadPosition(),
            Rotation = Rotation,
            HeadRotation = HeadRotation,
            PositionMode = 1,
            OnGround = false,
            RidingRuntimeId = 0,
            Tick = dimension.TickValue
        });
        Network.SendPacket(this, Attributes.CreateUpdate(RuntimeId, all: true, tick: dimension.TickValue)!);
        Network.SendPacket(this, Flags.CreateUpdate(RuntimeId, dimension.TickValue));
        Network.SendPacket(this, Abilities.CreateUpdate(UniqueId, Permissions));
    }

    public void Dispose() {
        lock (Sync) {
            if (Disposed) return;
            Disposed = true;
        }
    }
}
