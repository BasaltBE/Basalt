using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Network;
using Basalt.RakNet;
using Basalt.Containers;
using Basalt.Entity.Traits;
using Basalt.Entity.Traits.PlayerTraits;
using Basalt.Entity.Traits.Types;
using Basalt.Protocol.Types;
using Basalt.World;
using Basalt.World.Dimension;

namespace Basalt.Core;

public sealed class Player : Basalt.Entity.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly string Uuid;
    internal NetworkConnection? Connection;
    internal NetworkHandler? Network ;
    public PlayerAbilities Abilities { get; } = new();
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;
    public bool Spawned { get; private set; }
    public float Pitch;
    public float Yaw;
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public Dictionary<int, Container> openedContainers = [];

    public Player(string username, string xuid, string uuid) :
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        SetSpeed();
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

        UpdateAbilitiesPacket abilitiesPacket = new()
        {
            EntityUniqueId = UniqueId,
            Layers = [Abilities.ToLayer()]
        };

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

    public void Send(params DataPacket[] packets)
    {
        if (Connection is null || Network is null || packets.Length == 0)
        {
            return;
        }

        Network.SendPackets(Connection, packets);
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
