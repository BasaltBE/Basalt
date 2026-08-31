namespace Basalt.Core.Player;

using Basalt.Core.Network;
using Basalt.Core.Containers;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Events;
using Basalt.Core.Player.Traits;
using Basalt.Core.DDUI;
using Basalt.Core.Scoreboard;
using Basalt.Core.Entities;

using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Enums;

public class Player : Entity {
    public const ulong BucketCooldownTicks = 5;

    public override Vec3 GetEyePosition() {
        return new Vec3 {
            X = Position.X,
            Y = Position.Y + 1.62f,
            Z = Position.Z
        };
    }

    public readonly string Username;
    public readonly string Xuid;
    public readonly Guid Uuid;
    public BuildPlatform DeviceOS;
    public SerializedSkin Skin = new();
    internal string LastRequestedFullSkinId = string.Empty;
    internal NetworkConnection? Connection;
    internal NetworkHandler? Network;
    public PlayerAbilities Abilities { get; } = new();
    public PlayerPermissions Permissions { get; }
    public Dictionary<DisplaySlotType, Scoreboard> Scoreboards { get; } = [];
    public GameType Gamemode { get; private set; } = GameType.Survival;
    public bool IsOperator { get; internal set; }
    public bool Spawned { get; internal set; }
    public int ExperienceLevel { get; private set; }
    public int Experience { get; private set; }
    public int TotalExperience { get; private set; }
    public bool Grounded;
    public double LastInputQueueWaitMilliseconds { get; internal set; }
    public double LastInputProcessingMilliseconds { get; internal set; }
    private bool _applyingQueuedTeleport;
    public bool InitialAttributesSynced { get; internal set; }
    public float Pitch;
    public float Yaw;
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public ulong LastTeleportTick { get; private set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public ulong BucketCooldownTick;
    public Dictionary<ContainerId, Container> openedContainers = [];
    internal Dictionary<uint, PendingForm> PendingForms = [];
    internal Dictionary<string, DataDrivenScreen> Screens = [];

    public Player(string username, string xuid, Guid uuid) :
        this(EntityIdentifier.Player.ToIdentifierString(), username, xuid, uuid) {
    }

    protected Player(string identifier, string username, string xuid, Guid uuid) :
        base(identifier) {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        Permissions = new PlayerPermissions(this);
        Abilities.SetGamemode(Gamemode);

        Flags.SetActorFlag(ActorFlag.HasGravity, true);
        Flags.SetActorFlag(ActorFlag.Breathing, true);
        Flags.SetActorFlag(ActorFlag.CanShowName, true);
        Flags.SetActorFlag(ActorFlag.AlwaysShowName, true);
        Flags.SetActorFlag(ActorFlag.CanClimb, true);
        Metadata.SetActorMetadata(
            ActorDataId.Name,
            new ActorDataItem {
                Type = DataItemType.String,
                Value = Username
            }
        );
        Metadata.SetActorMetadata(
            ActorDataId.NametagAlwaysShow,
            new ActorDataItem {
                Type = DataItemType.Byte,
                Value = 1
            }
        );
        Metadata.SetActorMetadata(
            ActorDataId.PlayerFlags,
            new ActorDataItem {
                Type = DataItemType.Byte,
                Value = 0
            }
        );
    }

    public GameType GetGamemode() {
        return Gamemode;
    }

    public void SetDisplayName(string displayName) {
        Metadata.SetActorMetadata(
            ActorDataId.Name,
            new ActorDataItem {
                Type = DataItemType.String,
                Value = displayName
            }
        );
    }

    public void AddExperience(int amount, bool sound = false) {
        if (amount < 0) {
            RemoveExperience(-amount);
            return;
        }

        TotalExperience = Math.Min(int.MaxValue - amount, TotalExperience) + amount;
        int remaining = Experience + amount;
        while (remaining >= ExperienceToNextLevel(ExperienceLevel) && ExperienceLevel < 24791) {
            remaining -= ExperienceToNextLevel(ExperienceLevel);
            ExperienceLevel++;
        }

        Experience = Math.Min(remaining, ExperienceToNextLevel(ExperienceLevel) - 1);
        SyncExperience(sound);
    }

    public void RemoveExperience(int amount, bool sound = false) {
        if (amount <= 0) return;

        int total = ExperienceAtLevel(ExperienceLevel) + Experience;
        total = Math.Max(0, total - amount);
        ExperienceLevel = 0;
        Experience = 0;

        while (ExperienceLevel < 24791) {
            int required = ExperienceToNextLevel(ExperienceLevel);
            if (total < required) {
                Experience = total;
                break;
            }

            total -= required;
            ExperienceLevel++;
        }

        SyncExperience(sound);
    }

    public void AddExperienceLevels(int amount, bool sound = false) {
        ExperienceLevel = Math.Clamp(ExperienceLevel + amount, 0, 24791);
        Experience = Math.Min(Experience, ExperienceToNextLevel(ExperienceLevel) - 1);
        SyncExperience(sound);
    }

    public static int ExperienceToNextLevel(int level) {
        return level switch {
            < 16 => 7 + level * 2,
            < 31 => 37 + (level - 15) * 5,
            _ => 112 + (level - 30) * 9
        };
    }

    private static int ExperienceAtLevel(int level) {
        int total = 0;
        for (int current = 0; current < level; current++) {
            total += ExperienceToNextLevel(current);
        }

        return total;
    }

    private void SyncExperience(bool sound) {
        AttributeData? level = Attributes.GetAttribute(AttributeName.PlayerLevel);
        AttributeData? progress = Attributes.GetAttribute(AttributeName.PlayerExperience);
        if (level is null || progress is null) return;

        level.Current = ExperienceLevel;
        progress.Current = (float)Experience / ExperienceToNextLevel(ExperienceLevel);
        AttributesDirty = true;

        if (sound && ExperienceLevel > 0 && ExperienceLevel % 5 == 0) {
            PlaySound(SoundIdentifier.LevelUp, Position);
        }
    }

    public void SetGamemode(GameType gamemode) {
        Gamemode = gamemode;

        UpdatePlayerGameTypePacket gamemodePacket = new() {
            PlayerGameType = (int)gamemode,
            TargetPlayer = UniqueId
            ,
            Tick = Dimension?.World is Tickable tickable ? tickable.TickValue : 0
        };
        Abilities.SetGamemode(gamemode);

        UpdateAbilitiesPacket abilitiesPacket = Abilities.CreatePacket(UniqueId, IsOperator);

        Dimension?.Broadcast(gamemodePacket, new BroadcastOptions { Except = [this] });

        if (Dimension?.World?.Server is Server server && Connection is { } connection) {
            server.Network.QueuePacket(connection, new SetPlayerGameTypePacket { PlayerGameType = (int)gamemode });
            server.Network.QueuePacket(connection, abilitiesPacket);
        }
    }

    public void RestoreGamemode(GameType gamemode) {
        Gamemode = gamemode;
        Abilities.SetGamemode(gamemode);
        if (IsOperator) {
            Abilities.SetOperator(true);
        }
    }

    public void SetOperator(bool isOperator, bool syncClient = true) {
        Permissions.SetOperator(isOperator, syncClient);
    }

    public bool HasPermission(string permission) {
        return Permissions.Has(permission);
    }

    public Scoreboard GetScoreboard(DisplaySlotType slot, string title = "", ObjectiveSortOrder sortOrder = ObjectiveSortOrder.Descending) {
        if (Scoreboards.TryGetValue(slot, out Scoreboard? existing)) {
            return existing;
        }

        Scoreboard scoreboard = new(this, slot, title, sortOrder);
        Scoreboards[slot] = scoreboard;
        return scoreboard;
    }

    public void RemoveScoreboard(DisplaySlotType slot) {
        if (Scoreboards.Remove(slot, out Scoreboard? scoreboard)) {
            scoreboard.Hide();
        }
    }

    public override CompoundTag Write() {
        CompoundTag root = base.Write();
        root.Set("username", new StringTag { Value = Username });
        root.Set("xuid", new StringTag { Value = Xuid });
        root.Set("uuid", new StringTag { Value = Uuid.ToString() });
        root.Set("gamemode", new IntTag { Value = (int)Gamemode });
        root.Set("xp_level", new IntTag { Value = ExperienceLevel });
        root.Set("xp_progress", new IntTag { Value = Experience });
        root.Set("xp_total", new IntTag { Value = TotalExperience });

        if (Dimension?.World is not null) {
            root.Set("world", new StringTag { Value = Dimension.World.Name });
            root.Set("dimension", new StringTag { Value = Dimension.Identifier });
        }

        return root;
    }

    public override void Read(CompoundTag root) {
        base.Read(root);

        if (root.Get<IntTag>("gamemode") is { } gamemodeTag) {
            RestoreGamemode((GameType)gamemodeTag.Value);
        }

        ExperienceLevel = Math.Clamp(
            root.Get<IntTag>("xp_level")?.Value ?? root.Get<IntTag>("XpLevel")?.Value ?? 0,
            0,
            24791);
        Experience = Math.Max(
            0,
            root.Get<IntTag>("xp_progress")?.Value ?? root.Get<IntTag>("XpP")?.Value ?? 0);
        TotalExperience = Math.Max(
            0,
            root.Get<IntTag>("xp_total")?.Value ?? root.Get<IntTag>("XpTotal")?.Value ?? 0);
        Experience = Math.Min(Experience, ExperienceToNextLevel(ExperienceLevel) - 1);
        SyncExperience(false);

        SavedWorldName = root.Get<StringTag>("world")?.Value;
        SavedDimensionIdentifier = root.Get<StringTag>("dimension")?.Value;
    }

    /// <summary>
    /// The world name this player was in when last saved. Used during login to restore cross-world position.
    /// </summary>
    public string? SavedWorldName { get; private set; }

    /// <summary>
    /// The dimension identifier this player was in when last saved. Used during login to restore cross-world position.
    /// </summary>
    public string? SavedDimensionIdentifier { get; private set; }



    public void Send(params Packet[] packets) {
        if (Connection is null || Network is null || packets.Length == 0) {
            return;
        }

        Network.QueuePackets(Connection, packets);
    }

    public void PlaySound(
        SoundIdentifier soundEvent,
        Vec3? position = null,
        int data = 0,
        string actorIdentifier = "",
        bool babyMob = false,
        bool disableRelativeVolume = false,
        long uniqueActorId = 0,
        Vec3? fireAtPosition = null) {
        PlaySound(
            soundEvent.ToProtocolString(),
            position,
            data,
            actorIdentifier,
            babyMob,
            disableRelativeVolume,
            uniqueActorId,
            fireAtPosition);
    }

    public void PlaySound(
        string soundEvent,
        Vec3? position = null,
        int data = 0,
        string actorIdentifier = "",
        bool babyMob = false,
        bool disableRelativeVolume = false,
        long uniqueActorId = 0,
        Vec3? fireAtPosition = null) {
        Send(new LevelSoundEventPacket {
            SoundEvent = soundEvent,
            Position = position ?? Position,
            Data = data,
            ActorIdentifier = actorIdentifier,
            IsBaby = babyMob,
            IsGlobal = disableRelativeVolume,
            ActorUniqueId = uniqueActorId,
            FireAtPosition = fireAtPosition
        });
    }

    public bool DropItem(Item.ItemStack item) {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.DropItem(item) ?? false;
    }

    public ushort CollectItem(Item.ItemStack item) {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.CollectItem(item) ?? 0;
    }

    public void Disconnect(string reason = "", bool immediate = false) {
        if (Connection is null || Network is null) {
            return;
        }

        DisconnectPacket disconnect = new() {
            Reason = string.IsNullOrEmpty(reason)
                ? DisconnectFailReason.Disconnected
                : DisconnectFailReason.Kicked,
            Messages = string.IsNullOrEmpty(reason)
                ? null
                : new DisconnectPacketMessages {
                    Message = reason,
                    FilteredMessage = string.Empty
                }
        };

        if (immediate) {
            Network.SendPacket(Connection, disconnect);
        }
        else {
            Network.QueuePacket(Connection, disconnect);
        }
        Network.Disconnect(Connection);
    }



    public override void Spawn(Dimension dimension, EntitySpawnOptions options) {
        base.Spawn(dimension, options);
        if (Spawned) {
            Attributes.Send();
        }
    }

    public void Respawn() {
        if (IsAlive) {
            return;
        }

        if (Dimension is null) {
            return;
        }

        Dimension dimension = Dimension;
        if (dimension.World?.Server is Server server) {
            PlayerRespawnSignal signal = new(this);
            server.Emit(signal);
            if (!signal.Emit()) {
                return;
            }
        }

        Vec3 spawnPosition = dimension.SpawnPosition;
        IsSprinting = false;
        IsSneaking = false;
        IsSwimming = false;
        Flags.SetActorFlag(ActorFlag.Swimming, false);
        Flags.SetActorFlag(ActorFlag.Crawling, false);
        Flags.SetActorFlag(ActorFlag.Gliding, false);
        Flags.SetActorFlag(ActorFlag.UsingItem, false);
        Teleport(spawnPosition);
        Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
        dimension.UpdateEntityVisibility(this);

        ulong tick = Dimension.World is Tickable tickable ? tickable.TickValue : 0;
        dimension.Broadcast(new ActorEventPacket {
            ActorRuntimeId = RuntimeId ,
                EventId = 1,
            Data = 0
        }, new BroadcastOptions { Center = spawnPosition });
        Send(CreateActorDataPacket(tick));
        Attributes.Send();
    }

    public override void Teleport(Vec3 position, Dimension? dimension = null) {
        Dimension? previousDimension = Dimension;
        Dimension targetDimension = dimension ?? previousDimension ??
            throw new InvalidOperationException("Player must have a dimension to teleport without a target dimension.");

        Vec3 previousPosition = Location;
        bool changedDimension = previousDimension != targetDimension;
        bool changedDimensionType = previousDimension is not null && previousDimension.Type != targetDimension.Type;

        if (changedDimension && !_applyingQueuedTeleport &&
            previousDimension?.World?.Server is Server server) {
            server.QueuePlayerTransfer(this, previousDimension, targetDimension, position);
            return;
        }

        Location = position;
        Velocity = new Vec3();

        Vec3 eyePosition = GetEyePosition();

        ulong teleportTick = targetDimension.World is Tickable tp ? tp.TickValue : 0;
        LastTeleportTick = teleportTick;

        OnTeleport(new EntityTeleportOptions(previousPosition, position, changedDimension));

        if (changedDimension) {
            if (previousDimension is not null) {
                foreach (Player other in previousDimension.GetPlayers()) {
                    if (!ReferenceEquals(other, this)) {
                        Send(new RemoveActorPacket { ActorUniqueId = other.UniqueId });
                    }
                }
            }

            previousDimension?.RemovePlayer(this);
            previousDimension?.RemoveEntity(this, complete: false);
            Dimension = targetDimension;
            targetDimension.AddEntity(this);
        }

        ulong tick = targetDimension.World is Tickable tickable ? tickable.TickValue : 0;

        if (changedDimensionType) {
            Send(new ChangeDimensionPacket {
                DimensionId = (int)targetDimension.Type,
                Position = eyePosition,
                Respawn = true,
                LoadingScreenId = 0
            });
        }

        MovePlayerPacket movePlayer = new() {
            ActorRuntimeId = RuntimeId
            ,
            Position = eyePosition,
            Rotation = new Vec2 {
                X = Pitch,
                Y = Yaw
            },
            HeadRotation = HeadYaw,
            PositionMode = changedDimension ? (byte)1 : (byte)2,
            OnGround = false,
            RidingRuntimeId = 0
            ,
            TeleportData = changedDimension
                ? null
                : new MovePlayerTeleportData {
                    TeleportationCause = (int)TeleportCause.Command,
                    SourceActorType = 0
                },
            Tick = tick
        };
        Send(movePlayer);

        if (!changedDimension) {
            targetDimension.Broadcast(movePlayer, new BroadcastOptions {
                Except = [this]
            });
        }

        if (changedDimension) {
            Send(CreateActorDataPacket(tick));
            Send(Abilities.CreatePacket(UniqueId, IsOperator));
            GetTrait<PlayerChunkRenderingTrait>()?.StartChunkLoad();
            targetDimension.AddPlayer(this);
        }

        Attributes.Send();
    }

    internal void ApplyQueuedTeleport(Vec3 position, Dimension targetDimension) {
        _applyingQueuedTeleport = true;
        try {
            Teleport(position, targetDimension);
        }
        finally {
            _applyingQueuedTeleport = false;
        }
    }



    public void RegisterOpenContainer(ContainerId containerId, Container container) {
        openedContainers[containerId] = container;
    }

    public bool TryGetOpenContainer(ContainerId containerId, out Container? container) {
        return openedContainers.TryGetValue(containerId, out container);
    }

    public Container? GetContainer(FullContainerName name) {
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return null;
        }

        if (name.ContainerName == ContainerEnumName.ArmorContainer) {
            return GetTrait<EntityEquipmentTrait>()?.Armor;
        }

        if (name.ContainerName == ContainerEnumName.OffhandContainer) {
            return GetTrait<EntityEquipmentTrait>()?.Offhand;
        }

        if (name.ContainerName is ContainerEnumName.CombinedHotbarAndInventoryContainer
            or ContainerEnumName.InventoryContainer
            or ContainerEnumName.HotbarContainer) {
            return inventory.Container;
        }

        if (name.ContainerName == ContainerEnumName.CreatedOutputContainer) {
            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type == ContainerType.ANVIL) {
                    return candidate;
                }
            }

            return GetTrait<PlayerCursorTrait>()?.Container;
        }

        if (name.ContainerName == ContainerEnumName.CursorContainer) {
            return GetTrait<PlayerCursorTrait>()?.Container;
        }

        if (name.ContainerName == ContainerEnumName.CraftingInputContainer) {
            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type == ContainerType.WORKBENCH) {
                    return candidate;
                }
            }

            PlayerCraftingGridTrait? grid = GetTrait<PlayerCraftingGridTrait>();
            return grid?.Container;
        }

        if (name.ContainerName is ContainerEnumName.BarrelContainer
            or ContainerEnumName.AnvilInputContainer
            or ContainerEnumName.AnvilMaterialContainer
            or ContainerEnumName.AnvilResultPreviewContainer
            or ContainerEnumName.FurnaceFuelContainer
            or ContainerEnumName.FurnaceIngredientContainer
            or ContainerEnumName.FurnaceResultContainer
            or ContainerEnumName.BlastFurnaceIngredientContainer
            or ContainerEnumName.SmokerIngredientContainer
            or ContainerEnumName.LevelEntityContainer
            or ContainerEnumName.DynamicContainer) {

            if (name.DynamicId is uint dynamicId && dynamicId != 0 &&
                TryGetOpenContainer((ContainerId)unchecked((sbyte)(byte)dynamicId), out Container? containerById)) {
                return containerById;
            }

            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type != ContainerType.INVENTORY) {
                    return candidate;
                }
            }

            return name.ContainerName == ContainerEnumName.BarrelContainer
                ? inventory.Container
                : null;
        }

        if (name.DynamicId is uint otherDynamicId && otherDynamicId != 0 &&
            TryGetOpenContainer((ContainerId)unchecked((sbyte)(byte)otherDynamicId), out Container? container)) {
            return container;
        }

        return null;
    }


    public override void SpawnTo(Player player, ulong tick, Vec3? position = null) {
        Vec3 spawnPosition = position ?? GetEyePosition();
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        Item.ItemStack? held = inventory?.GetHeldItem();

        NetworkItemStackDescriptor carriedItem =
            held?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor();

        player.Send(new AddPlayerPacket {
            BuildPlatform = (int)DeviceOS,
            DeviceId = string.Empty,
            ActorLinks = [],
            AbilitiesData = new SerializedAbilitiesData {
                    CommandPermissions = IsOperator ? (byte)2 : (byte)0,
                Layers = [
                    Abilities.ToLayer()
                ],
                PlayerPermissions = (sbyte)(IsOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member),
                TargetPlayerRawId = UniqueId
            },
            SynchedProperties = new PropertySyncData {
                FloatEntries = [],
                IntEntries = []
            },
            EntityData = CreateActorDataPacket(tick).ActorData,
            PlayerGameType = (int)Gamemode,
            CarriedItem = carriedItem,
            Rotation = new Vec2 {
                X = Pitch,
                Y = Yaw
            },
            Velocity = new Vec3(),
            Position = spawnPosition,
            PlatformChatId = string.Empty,
            ActorRuntimeId = RuntimeId
            ,
            PlayerName = Username,
            Uuid = GetUUID(),
            HeadRotation = HeadYaw
        });
    }

    public void SendMessage(string message) {
        TextPacket packet = new() {
            Body = new TextPacketBody {
                MessageType = TextPacketType.Raw,
                Message = message
            }
        };

        Send(packet);
    }

    public Basalt.BedrockProtocol.Types.Uuid GetUUID() {
        return NetworkIo.FromGuid(Uuid);
    }
}






