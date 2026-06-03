namespace Basalt.Server.Player;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Server.Network;
using Basalt.RakNet;
using Basalt.Server.Containers;
using Basalt.Protocol.Types;
using Basalt.Protocol.Nbt;
using Basalt.Server.World;
using Basalt.Server.World.Dimension;
using Basalt.Binary;
using Basalt.Server.Entity.Traits;
using Basalt.Server.Entity.Traits.Types;
using Basalt.Server.Player.Traits;

public sealed class Player : Entity.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly Guid Uuid;
    public DeviceOS DeviceOS;
    private byte[]? Skin;
    internal NetworkConnection? Connection;
    internal NetworkHandler? Network;
    public PlayerAbilities Abilities { get; } = new();
    public HashSet<string> Permissions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;
    public bool IsOperator { get; private set; }
    public bool Spawned { get; private set; }
    public float Pitch;
    public float Yaw;
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public Dictionary<int, Container> openedContainers = [];

    public Player(string username, string xuid, Guid uuid) :
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;

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

        UpdateAbilitiesPacket abilitiesPacket = CreateAbilitiesPacket();

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

    public void LoadGamemode(Gamemode gamemode)
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
        IsOperator = isOperator;
        Abilities.SetOperator(isOperator);
        if (isOperator)
        {
            Permissions.Add("basalt.op");
        }
        else
        {
            Permissions.Remove("basalt.op");
        }

        if (!syncClient || Connection is null || Network is null)
        {
            return;
        }

        Network.SendPacket(Connection, CreateAbilitiesPacket());

        if (Dimension?.World?.Server is global::Basalt.Server.Server server)
        {
            server.Commands.SendAvailableCommands(server, this);
        }
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public new CompoundTag WriteToNbt()
    {
        CompoundTag root = base.WriteToNbt();
        root.Set("username", new StringTag { Value = Username });
        root.Set("xuid", new StringTag { Value = Xuid });
        root.Set("uuid", new StringTag { Value = Uuid.ToString() });
        root.Set("gamemode", new IntTag { Value = (int)Gamemode });
        root.Set("isOp", new ByteTag { Value = IsOperator ? (sbyte)1 : (sbyte)0 });
        return root;
    }

    public new void FromNBT(CompoundTag root)
    {
        base.FromNBT(root);

        if (root.Get<IntTag>("gamemode") is { } gamemodeTag)
        {
            LoadGamemode((Gamemode)gamemodeTag.Value);
        }

        IsOperator = (root.Get<ByteTag>("isOp")?.Value ?? 0) != 0;
        Abilities.SetOperator(IsOperator);
        if (IsOperator)
        {
            Permissions.Add("basalt.op");
        }
        else
        {
            Permissions.Remove("basalt.op");
        }
    }

    UpdateAbilitiesPacket CreateAbilitiesPacket()
    {
        return new UpdateAbilitiesPacket
        {
            EntityUniqueId = UniqueId,
            PlayerPermission = IsOperator ? PlayerPermissionLevel.Operator : PlayerPermissionLevel.Member,
            CommandPermission = IsOperator ? CommandPermissionLevel.Admin : CommandPermissionLevel.Any,
            Layers = [Abilities.ToLayer()]
        };
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
        if (Dimension is null || item.StackSize == 0 || item.Type == Item.ItemType.Air)
        {
            return false;
        }

        Vec3f feet = GetPosition();
        float yaw = MathF.PI / 180f * Yaw;
        float pitch = MathF.PI / 180f * Pitch;

        global::Basalt.Server.Entity.ItemEntity drop = new(item)
        {
            Position = new Vec3f
            {
                X = feet.X,
                Y = feet.Y + 1.15f,
                Z = feet.Z
            },
            Velocity = new Vec3f
            {
                X = (-MathF.Sin(yaw) * MathF.Cos(pitch)) / 3f,
                Y = ((-MathF.Sin(pitch)) / 2f) + 0.2f,
                Z = (MathF.Cos(yaw) * MathF.Cos(pitch)) / 3f
            }
        };

        ulong currentTick = Dimension.World is Tickable tickable ? tickable.TickValue : 0;
        drop.LockMergeUntil(currentTick + 50);
        drop.LockPickupUntil(currentTick + 50);
        drop.Spawn(Dimension, new EntitySpawnOptions(InitialSpawn: false));
        return true;
    }

    public ushort CollectItem(Item.ItemStack item)
    {
        var inventory = GetTrait<EntityInventoryTrait>();
        if (inventory is null || item.StackSize == 0)
        {
            return 0;
        }

        var container = inventory.Container;
        ushort remaining = item.StackSize;
        ushort moved = 0;

        for (int i = 0; i < container.GetSize() && remaining > 0; i++)
        {
            Item.ItemStack? existing = container.GetItem(i);
            if (existing is null || !existing.CanStackWith(item) || existing.StackSize >= existing.Type.MaxStackSize)
            {
                continue;
            }

            int space = existing.Type.MaxStackSize - existing.StackSize;
            int transfer = Math.Min(space, remaining);
            if (transfer <= 0)
            {
                continue;
            }

            existing.IncrementStack((ushort)transfer);
            container.UpdateSlot(i);
            remaining = (ushort)(remaining - transfer);
            moved = (ushort)(moved + transfer);
        }

        for (int i = 0; i < container.GetSize() && remaining > 0; i++)
        {
            if (container.GetItem(i) is not null)
            {
                continue;
            }

            ushort transfer = (ushort)Math.Min(remaining, item.Type.MaxStackSize);
            Item.ItemStack stack = item.Clone(transfer);
            container.SetItem(i, stack);
            remaining = (ushort)(remaining - transfer);
            moved = (ushort)(moved + transfer);
        }

        if (moved == 0)
        {
            return 0;
        }

        item.SetStackSize(remaining);
        inventory.SyncToPlayer(this);
        return moved;
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

    public void SetSpawned(bool spawned)
    {
        Spawned = true;
    }

    public override void Spawn(Dimension dimension, EntitySpawnOptions options)
    {
        base.Spawn(dimension, options);
        SendAttributes();
    }

    public void Teleport(Vec3f position, Dimension? dimension = null)
    {
        Dimension? previousDimension = Dimension;
        Dimension targetDimension = dimension ?? previousDimension ??
            throw new InvalidOperationException("Player must have a dimension to teleport without a target dimension.");

        Vec3f previousPosition = Position;
        bool changedDimension = previousDimension != targetDimension;
        bool changedDimensionType = previousDimension is not null && previousDimension.Type != targetDimension.Type;

        Position = position;
        OnTeleport(new EntityTeleportOptions(previousPosition, position));

        if (changedDimension)
        {
            previousDimension?.Broadcast(new RemoveActorPacket
            {
                EntityUniqueId = UniqueId
            }, new BroadcastOptions { Except = [this] });

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
        else
        {
            Send(new MovePlayerPacket
            {
                RuntimeId = RuntimeId,
                Position = position,
                Pitch = Pitch,
                Yaw = Yaw,
                HeadYaw = HeadYaw,
                Mode = MoveMode.Teleport,
                OnGround = false,
                RiddenRuntimeId = 0,
                TeleportCause = TeleportCause.Command,
                TeleportSourceEntityType = 0,
                Tick = tick
            });
        }

        if (changedDimension)
        {
            targetDimension.Broadcast(CreateActorDataPacket(tick), new BroadcastOptions { Except = [this] });
        }

        GetTrait<PlayerChunkRenderingTrait>()?.StartChunkLoad();
        SendAttributes();
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

        if (name.ContainerId == (byte)ContainerId.Cursor)
        {
            PlayerCursorTrait? cursor = GetTrait<PlayerCursorTrait>();
            return cursor?.Container;
        }

        if (name.DynamicContainerId.HasValue && TryGetOpenContainer((int)name.DynamicContainerId.Value!, out Container? container))
        {
            return container;
        }

        return null;
    }


    public void SendAttributes()
    {
        if (Network == null || Connection == null)
        {
            return;
        }

        ulong tick = Dimension?.World is Tickable tickable ? tickable.TickValue : 0;

        UpdateAttributesPacket attributes = new()
        {
            RuntimeId = RuntimeId,
            Tick = tick,
            Attributes = Attributes.GetAll().ToList()
        };

        if (attributes.Attributes.Count > 0)
        {
            Network.SendPacket(Connection, attributes);
        }
    }

    public PlayerListEntry CreatePlayerListEntry()
    {
        global::Basalt.Protocol.Types.Skin skin = new();
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

    public void SetSkin(global::Basalt.Protocol.Types.Skin skin)
    {
        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        Binary.BinaryWriter writer = stream;
        skin.Write(writer);
        Skin = writer.GetProcessedBytes().ToArray();
    }

    public override void SpawnTo(Player player, ulong tick)
    {
        ItemInstance heldItem = new();
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        Item.ItemStack? held = inventory?.GetHeldItem();
        if (held is not null)
        {
            heldItem.Stack = held.ToNetworkStack();
            heldItem.StackNetworkId = held.NetworkStackId;
        }

        // return;
        player.Send(new AddPlayerPacket
        {
            Uuid = Uuid,
            Username = Username,
            EntityRuntimeId = RuntimeId,
            PlatformChatId = string.Empty,
            Position = Position,
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






