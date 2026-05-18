using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Network;
using Basalt.RakNet;
using Basalt.Containers;
using Basalt.Entity.Traits;
using Basalt.Entity.Traits.PlayerTraits;
using Basalt.Protocol.Types;

namespace Basalt.Core;

public sealed class Player : Basalt.Entity.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly string Uuid;
    internal NetworkConnection? Connection { get; set; }
    internal NetworkHandler? Network { get; set; }
    public PlayerAbilities Abilities { get; } = new();
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;
    public bool Spawned { get; private set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float HeadYaw { get; set; }
    public BlockPos? BreakingBlock { get; set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    public Dictionary<int, Container> openedContainers = [];

    public Player( string username, string xuid, string uuid) : 
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

        if (name.ContainerId == (byte)ContainerId.InventoryUi)
        {
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

        if (name.ContainerId == (byte)ContainerId.DynamicContainer)
        {
            foreach ((int _, Container candidate) in openedContainers)
            {
                if (!ReferenceEquals(candidate, inventory.Container))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

}
