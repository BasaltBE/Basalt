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

public sealed class Player : Entities.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly Guid Uuid;
    public DeviceOS DeviceOS;
    private byte[]? Skin;
    internal NetworkConnection? Connection;
    internal NetworkHandler? Network;
    public PlayerAbilities Abilities { get; } = new();
    public PlayerPermissions Permissions { get; }
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;
    public bool IsOperator { get; internal set; }
    public bool Spawned { get; internal set; }
    public float Pitch;
    public float Yaw;
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public Dictionary<int, Container> openedContainers = [];
    internal Dictionary<int, PendingForm> PendingForms = [];
    internal Dictionary<string, DataDrivenScreen> Screens = [];

    public Player(string username, string xuid, Guid uuid) :
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        Permissions = new PlayerPermissions(this);

        Flags.SetActorFlag(ActorFlag.HasGravity, true);
        Flags.SetActorFlag(ActorFlag.Breathing, true);
        Flags.SetActorFlag(ActorFlag.CanShowName, true);
        Flags.SetActorFlag(ActorFlag.AlwaysShowName, true);
    }

    public Gamemode GetGamemode()
    {
        return Gamemode;
    }

    public void SetGamemode(Gamemode gamemode)
    {
        Gamemode = gamemode;

        UpdatePlayerGameTypePacket gamemodePacket = new()
        {
            GameType = gamemode,
            PlayerUniqueId = UniqueId,
            Tick = Dimension?.World is Tickable tickable ? tickable.TickValue : 0
        };
        Abilities.SetGamemode(gamemode);

        UpdateAbilitiesPacket abilitiesPacket = Abilities.CreatePacket(UniqueId, IsOperator);

        Dimension?.Broadcast(gamemodePacket, new BroadcastOptions { Except = [this] });

        if (Dimension?.World?.Server is Server server)
        {
            foreach ((NetworkConnection connection, Player player) in server.Players)
            {
                if (ReferenceEquals(player, this))
                {
                    server.Network.SendPacket(connection, new SetPlayerGameTypePacket { GameType = gamemode });
                    server.Network.SendPacket(connection, abilitiesPacket);
                    break;
                }
            }
        }
    }

    public void RestoreGamemode(Gamemode gamemode)
    {
        Gamemode = gamemode;
        Abilities.SetGamemode(gamemode);
        if (IsOperator)
        {
            Abilities.SetOperator(true);
        }
    }

    public void SetOperator(bool isOperator, bool syncClient = true)
    {
        Permissions.SetOperator(isOperator, syncClient);
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Has(permission);
    }

    public new CompoundTag Write()
    {
        CompoundTag root = base.Write();
        root.Set("username", new StringTag { Value = Username });
        root.Set("xuid", new StringTag { Value = Xuid });
        root.Set("uuid", new StringTag { Value = Uuid.ToString() });
        root.Set("gamemode", new IntTag { Value = (int)Gamemode });
        root.Set("isOp", new ByteTag { Value = IsOperator ? (sbyte)1 : (sbyte)0 });
        return root;
    }

    public new void Read(CompoundTag root)
    {
        base.Read(root);

        if (root.Get<IntTag>("gamemode") is { } gamemodeTag)
        {
            RestoreGamemode((Gamemode)gamemodeTag.Value);
        }

        IsOperator = (root.Get<ByteTag>("isOp")?.Value ?? 0) != 0;
        Permissions.RestoreOperator(IsOperator);
    }



    public void Send(params DataPacket[] packets)
    {
        if (Connection is null || Network is null || packets.Length == 0)
        {
            return;
        }

        Network.SendPackets(Connection, packets);
    }

    public bool DropItem(Item.ItemStack item)
    {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.DropItem(item) ?? false;
    }

    public ushort CollectItem(Item.ItemStack item)
    {
        var inventory = GetTrait<EntityInventoryTrait>();
        return inventory?.CollectItem(item) ?? 0;
    }

    public void Disconnect(string reason = "")
    {
        if (Connection is null || Network is null)
        {
            return;
        }

        DisconnectPacket disconnect = new()
        {
            Reason = string.IsNullOrEmpty(reason) ? DisconnectReason.Disconnected : DisconnectReason.NetherNetSignalingSigninFailed,
            HideDisconnectionScreen = string.IsNullOrEmpty(reason),
            Message = reason,
            FilteredMessage = string.Empty
        };

        Network.SendPacket(Connection, disconnect, immediate: true);
        Connection.Disconnect();
    }



    public override void Spawn(Dimension dimension, EntitySpawnOptions options)
    {
        base.Spawn(dimension, options);
        Attributes.Send();
    }

    public void Respawn()
    {
        if (IsAlive || Dimension is null)
        {
            return;
        }

        Vec3f spawnPosition = Location;
        Spawn(Dimension, new EntitySpawnOptions(InitialSpawn: false));
        Location = spawnPosition;

        ulong tick = Dimension.World is Tickable tickable ? tickable.TickValue : 0;

        Send(new RespawnPacket
        {
            Position = spawnPosition,
            State = RespawnState.ReadyToSpawn,
            EntityRuntimeId = RuntimeId
        });

        Send(CreateActorDataPacket(tick));
        Attributes.Send();
    }

    public void Teleport(Vec3f position, Dimension? dimension = null)
    {
        Dimension? previousDimension = Dimension;
        Dimension targetDimension = dimension ?? previousDimension ??
            throw new InvalidOperationException("Player must have a dimension to teleport without a target dimension.");

        Vec3f previousPosition = Location;
        bool changedDimension = previousDimension != targetDimension;
        bool changedDimensionType = previousDimension is not null && previousDimension.Type != targetDimension.Type;

        Location = position;
        Velocity = new Vec3f();
        OnTeleport(new EntityTeleportOptions(previousPosition, position));

        if (changedDimension)
        {
            if (previousDimension?.World?.Server is Server dimServer)
            {
                foreach ((_, Player other) in dimServer.Players)
                {
                    if (ReferenceEquals(other, this) || other.Dimension != previousDimension)
                    {
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

        if (changedDimensionType)
        {
            Send(new ChangeDimensionPacket
            {
                Dimension = targetDimension.Type,
                Position = position,
                Respawn = true,
                HasLoadingScreen = false
            });
        }

        Send(new MovePlayerPacket
        {
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
        });

        if (changedDimension)
        {
            Send(CreateActorDataPacket(tick));
            Send(Abilities.CreatePacket(UniqueId, IsOperator));
            targetDimension.AddPlayer(this);
        }

        GetTrait<PlayerChunkRenderingTrait>()?.StartChunkLoad();
        Attributes.Send();
    }



    public void RegisterOpenContainer(int windowId, Container container)
    {
        openedContainers[windowId] = container;
    }

    public bool TryGetOpenContainer(int windowId, out Container? container)
    {
        return openedContainers.TryGetValue(windowId, out container);
    }

    public Container? GetContainer(FullContainerName name)
    {
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return null;
        }

        if (name.ContainerId is (byte)ContainerId.Armor or 12 or (byte)ContainerId.Inventory or (byte)ContainerId.Hotbar or (byte)ContainerId.FixedInventory or (byte)ContainerId.Offhand)
        {
            return inventory.Container;
        }

        if (name.ContainerId == (byte)ContainerId.Barrel || name.ContainerId == (byte)ContainerId.InventoryUi)
        {
            if (name.DynamicContainerId.HasValue && TryGetOpenContainer((int)name.DynamicContainerId.Value!, out Container? containerById))
            {
                return containerById;
            }

            foreach ((int _, Container candidate) in openedContainers)
            {
                if (candidate.Type != ContainerType.Inventory)
                {
                    return candidate;
                }
            }

            return inventory.Container;
        }

        if (name.ContainerId is (byte)ContainerId.Cursor or (byte)ContainerId.CreatedOutput)
        {
            PlayerCursorTrait? cursor = GetTrait<PlayerCursorTrait>();
            return cursor?.Container;
        }

        if (name.ContainerId == (byte)ContainerId.CraftingInput)
        {
            foreach ((int _, Container candidate) in openedContainers)
            {
                if (candidate.Type == ContainerType.Workbench)
                {
                    return candidate;
                }
            }

            Traits.PlayerCraftingGridTrait? grid = GetTrait<Traits.PlayerCraftingGridTrait>();
            return grid?.Container;
        }

        if (name.DynamicContainerId.HasValue && TryGetOpenContainer((int)name.DynamicContainerId.Value!, out Container? container))
        {
            return container;
        }

        return null;
    }


    public PlayerListEntry CreatePlayerListEntry()
    {
        Skin skin = new();
        if (Skin is not null && Skin.Length > 0)
        {
            int offset = 0;
            Binary.BinaryReader reader = new(Skin, ref offset);
            skin.Read(reader);
        }

        return new PlayerListEntry
        {
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

    public void SetSkin(Skin skin)
    {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        Binary.BinaryWriter writer = stream;
        skin.Write(writer);
        Skin = writer.GetProcessedBytes().ToArray();
    }

    public override void SpawnTo(Player player, ulong tick, Vec3f? position = null)
    {
        Vec3f spawnPosition = position ?? Location;
        ItemInstance heldItem = new();
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        Item.ItemStack? held = inventory?.GetHeldItem();
        if (held is not null)
        {
            heldItem.Stack = held.ToNetworkStack();
            heldItem.StackNetworkId = held.NetworkStackId;
        }

        player.Send(new AddPlayerPacket
        {
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
            AbilityData = new AbilityData
            {
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
    )
    {
        var packet = new TextPacket()
        {
            VariantType = TextVariantType.MessageOnly,
            FilteredMessage = null,
            NeedsTranslation = false,
            Xuid = "",
            PlatformChatId = "",
            Variant = new TextVariant()
            {
                Message = message,
                Parameters = new List<string>(),
                Source = "",
                Type = TextType.Raw,
            }
        };

        Send(packet);
    }

}






