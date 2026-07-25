namespace Basalt.Core.Player;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Core.Network;
using Basalt.RakNet;
using Basalt.Core.Containers;
using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Binary;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player.Traits;
using Basalt.Core.DDUI;
using Basalt.Core.Scoreboard;

public sealed class Player : Entities.Entity {
    public readonly string Username;
    public readonly string Xuid;
    public readonly Guid Uuid;
    public DeviceOS DeviceOS;
    private byte[]? Skin;
    internal NetworkConnection? Connection;
    internal NetworkHandler? Network;
    public PlayerAbilities Abilities { get; } = new();
    public PlayerPermissions Permissions { get; }
    public Dictionary<DisplaySlotType, Scoreboard> Scoreboards { get; } = [];
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;
    public bool IsOperator { get; internal set; }
    public bool Spawned { get; internal set; }
    public float Pitch;
    public float Yaw;
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public ulong LastTeleportTick { get; private set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public Dictionary<ContainerId, Container> openedContainers = [];
    internal Dictionary<int, PendingForm> PendingForms = [];
    internal Dictionary<string, DataDrivenScreen> Screens = [];

    public Player(string username, string xuid, Guid uuid) :
        base(EntityIdentifier.Player.ToIdentifierString()) {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        Permissions = new PlayerPermissions(this);
        Abilities.SetGamemode(Gamemode);

        Flags.SetActorFlag(ActorFlag.HasGravity, true);
        Flags.SetActorFlag(ActorFlag.Breathing, true);
        Flags.SetActorFlag(ActorFlag.CanShowName, true);
        Flags.SetActorFlag(ActorFlag.AlwaysShowName, true);
    }

    public Gamemode GetGamemode() {
        return Gamemode;
    }

    public void SetGamemode(Gamemode gamemode) {
        Gamemode = gamemode;

        UpdatePlayerGameTypePacket gamemodePacket = new() {
            GameType = gamemode,
            PlayerUniqueId = UniqueId,
            Tick = Dimension?.World is Tickable tickable ? tickable.TickValue : 0
        };
        Abilities.SetGamemode(gamemode);

        UpdateAbilitiesPacket abilitiesPacket = Abilities.CreatePacket(UniqueId, IsOperator);

        Dimension?.Broadcast(gamemodePacket, new BroadcastOptions { Except = [this] });

        if (Dimension?.World?.Server is Server server) {
            foreach ((NetworkConnection connection, Player player) in server.Players) {
                if (ReferenceEquals(player, this)) {
                    server.Network.QueuePacket(connection, new SetPlayerGameTypePacket { GameType = gamemode });
                    server.Network.QueuePacket(connection, abilitiesPacket);
                    break;
                }
            }
        }
    }

    public void RestoreGamemode(Gamemode gamemode) {
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

    public new CompoundTag Write() {
        CompoundTag root = base.Write();
        root.Set("username", new StringTag { Value = Username });
        root.Set("xuid", new StringTag { Value = Xuid });
        root.Set("uuid", new StringTag { Value = Uuid.ToString() });
        root.Set("gamemode", new IntTag { Value = (int)Gamemode });

        if (Dimension?.World is not null) {
            root.Set("world", new StringTag { Value = Dimension.World.Name });
            root.Set("dimension", new StringTag { Value = Dimension.Identifier });
        }

        return root;
    }

    public new void Read(CompoundTag root) {
        base.Read(root);

        if (root.Get<IntTag>("gamemode") is { } gamemodeTag) {
            RestoreGamemode((Gamemode)gamemodeTag.Value);
        }

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



    public void Send(params DataPacket[] packets) {
        if (Connection is null || Network is null || packets.Length == 0) {
            return;
        }

        Network.QueuePackets(Connection, packets);
    }

    public bool DropItem(Item.ItemStack item) {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.DropItem(item) ?? false;
    }

    public ushort CollectItem(Item.ItemStack item) {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.CollectItem(item) ?? 0;
    }

    public void Disconnect(string reason = "") {
        if (Connection is null || Network is null) {
            return;
        }

        DisconnectPacket disconnect = new() {
            Reason = string.IsNullOrEmpty(reason) ? DisconnectReason.Disconnected : DisconnectReason.NetherNetSignalingSigninFailed,
            HideDisconnectionScreen = string.IsNullOrEmpty(reason),
            Message = reason,
            FilteredMessage = string.Empty
        };

        Network.QueuePacket(Connection, disconnect);
        Connection.Disconnect();
    }



    public override void Spawn(Dimension dimension, EntitySpawnOptions options) {
        base.Spawn(dimension, options);
        if (Spawned) {
            Attributes.Send();
        }
    }

    public void Respawn() {
        if (IsAlive || Dimension is null) {
            return;
        }

        Vec3f spawnPosition = Location;
        Spawn(Dimension, new EntitySpawnOptions(InitialSpawn: false));
        Location = spawnPosition;

        ulong tick = Dimension.World is Tickable tickable ? tickable.TickValue : 0;

        Send(new RespawnPacket {
            Position = spawnPosition,
            State = RespawnState.ReadyToSpawn,
            EntityRuntimeId = RuntimeId
        });

        Send(CreateActorDataPacket(tick));
        Attributes.Send();
    }

    public void Teleport(Vec3f position, Dimension? dimension = null) {
        Dimension? previousDimension = Dimension;
        Dimension targetDimension = dimension ?? previousDimension ??
            throw new InvalidOperationException("Player must have a dimension to teleport without a target dimension.");

        Vec3f previousPosition = Location;
        bool changedDimension = previousDimension != targetDimension;
        bool changedDimensionType = previousDimension is not null && previousDimension.Type != targetDimension.Type;

        Location = position;
        Velocity = new Vec3f();

        ulong teleportTick = targetDimension.World is Tickable tp ? tp.TickValue : 0;
        LastTeleportTick = teleportTick;

        OnTeleport(new EntityTeleportOptions(previousPosition, position, changedDimension));

        if (changedDimension) {
            if (previousDimension?.World?.Server is Server dimServer) {
                foreach ((_, Player other) in dimServer.Players) {
                    if (ReferenceEquals(other, this) || other.Dimension != previousDimension) {
                        continue;
                    }

                    Send(new RemoveActorPacket { EntityUniqueId = other.UniqueId });
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
                Dimension = targetDimension.Type,
                Position = position,
                Respawn = true,
                HasLoadingScreen = false
            });
        }

        MovePlayerPacket movePlayer = new() {
            RuntimeId = RuntimeId,
            Position = position,
            Pitch = Pitch,
            Yaw = Yaw,
            HeadYaw = HeadYaw,
            Mode = changedDimension ? MoveMode.Reset : MoveMode.Teleport,
            OnGround = false,
            RiddenRuntimeId = 0,
            TeleportCause = TeleportCause.Command,
            TeleportSourceEntityType = 0,
            Tick = tick
        };
        Send(movePlayer);

        if (!changedDimension) {
            targetDimension.Broadcast(movePlayer, new BroadcastOptions {
                Radius = float.PositiveInfinity,
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

        if (name.ContainerId == (byte)ContainerName.Armor) {
            EntityEquipmentTrait? equipment = GetTrait<EntityEquipmentTrait>();
            return equipment?.Armor;
        }

        if (name.ContainerId == (byte)ContainerName.Offhand) {
            EntityEquipmentTrait? equipment = GetTrait<EntityEquipmentTrait>();
            return equipment?.Offhand;
        }

        if (name.ContainerId is (byte)ContainerName.CombinedHotbarAndInventory
            or (byte)ContainerName.Inventory or (byte)ContainerName.Hotbar) {
            return inventory.Container;
        }

        if (name.ContainerId == (byte)ContainerName.Barrel) {
            if (name.DynamicContainerId.HasValue && TryGetOpenContainer((ContainerId)(sbyte)name.DynamicContainerId.Value, out Container? containerById)) {
                return containerById;
            }

            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type != ContainerType.Inventory) {
                    return candidate;
                }
            }

            return inventory.Container;
        }

        if (name.ContainerId is (byte)ContainerName.Cursor or (byte)ContainerName.CreatedOutput) {
            PlayerCursorTrait? cursor = GetTrait<PlayerCursorTrait>();
            return cursor?.Container;
        }

        if (name.ContainerId == (byte)ContainerName.LevelEntity) {
            if (name.DynamicContainerId.HasValue && TryGetOpenContainer((ContainerId)(sbyte)name.DynamicContainerId.Value, out Container? containerById)) {
                return containerById;
            }

            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type != ContainerType.Inventory) {
                    return candidate;
                }
            }

            return null;
        }

        if (name.ContainerId == (byte)ContainerName.CraftingInput) {
            foreach ((ContainerId _, Container candidate) in openedContainers) {
                if (candidate.Type == ContainerType.Workbench) {
                    return candidate;
                }
            }

            Traits.PlayerCraftingGridTrait? grid = GetTrait<Traits.PlayerCraftingGridTrait>();
            return grid?.Container;
        }

        if (name.DynamicContainerId.HasValue && TryGetOpenContainer((ContainerId)(sbyte)name.DynamicContainerId.Value, out Container? container)) {
            return container;
        }

        return null;
    }


    public PlayerListEntry CreatePlayerListEntry() {
        Skin skin = new();
        if (Skin is not null && Skin.Length > 0) {
            int offset = 0;
            Binary.BinaryReader reader = new(Skin, ref offset);
            skin.Read(reader);
        }

        return new PlayerListEntry {
            Uuid = Uuid,
            EntityUniqueId = UniqueId,
            Username = Username,
            Xuid = Xuid,
            PlatformChatId = string.Empty,
            DeviceOS = DeviceOS,
            Skin = skin,
            Teacher = false,
            Host = false,
            SubClient = false,
            PlayerColor = 0
        };
    }

    public void SetSkin(Skin skin) {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        Binary.BinaryWriter writer = stream;
        skin.Write(writer);
        Skin = writer.GetProcessedBytes().ToArray();
    }

    public override void SpawnTo(Player player, ulong tick, Vec3f? position = null) {
        Vec3f spawnPosition = position ?? Location;
        ItemInstance heldItem = new();
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();

        Item.ItemStack? held = inventory?.GetHeldItem();
        if (held is not null) {
            heldItem.Stack = held.ToNetworkStack();
            heldItem.StackNetworkId = held.NetworkStackId;
        }

        player.Send(new AddPlayerPacket {
            Uuid = Uuid,
            Username = Username,
            EntityRuntimeId = RuntimeId,
            PlatformChatId = string.Empty,
            Position = spawnPosition,
            Velocity = new Vec3f(),
            Pitch = Pitch,
            Yaw = Yaw,
            HeadYaw = HeadYaw,
            HeldItem = heldItem,
            GameType = (int)Gamemode,
            EntityMetadata = CreateActorDataPacket(tick).Metadata,
            EntityProperties = new EntityProperties(),
            AbilityData = new AbilityData {
                EntityUniqueId = UniqueId,
                Layers = [Abilities.ToLayer()]
            },
            EntityLinks = [],
            DeviceId = string.Empty,
            DeviceOS = DeviceOS
        });
    }

    public void SendMessage(
        string message
    ) {
        var packet = new TextPacket() {
            VariantType = TextVariantType.MessageOnly,
            FilteredMessage = null,
            NeedsTranslation = false,
            Xuid = "",
            PlatformChatId = "",
            Variant = new TextVariant() {
                Message = message,
                Parameters = new List<string>(),
                Source = "",
                Type = TextType.Raw,
            }
        };

        Send(packet);
    }

}






