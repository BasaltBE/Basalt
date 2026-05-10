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
    public BlockPos? BreakingBlock { get; set; }
    public BlockPos? LastActionBlockPosition { get; set; }
    public BlockPos? LastActionResultPosition { get; set; }
    public int LastActionFace { get; set; }
    private readonly Dictionary<int, Container> _openContainers = [];

    public Player( string username, string xuid, string uuid) : 
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        Flags.SetActorFlag(ActorFlag.HasGravity, true);
        Flags.SetActorFlag(ActorFlag.Breathing, true);
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
        _openContainers[windowId] = container;
    }

    public void UnregisterOpenContainer(int windowId)
    {
        _openContainers.Remove(windowId);
    }

    public bool TryGetOpenContainer(int windowId, out Container? container)
    {
        return _openContainers.TryGetValue(windowId, out container);
    }

    public Container? GetContainer(FullContainerName name)
    {
        EntityInventoryTrait? inventory = GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return null;
        }

        if (name.ContainerId is 6 or 12 or 27 or 28 or 29 or 33)
        {
            return inventory.Container;
        }

        if (name.ContainerId is 58 or 59)
        {
            PlayerCursorTrait? cursor = GetTrait<PlayerCursorTrait>();
            return cursor?.Container;
        }

        if (name.DynamicContainerId.HasValue && TryGetOpenContainer((int)name.DynamicContainerId.Value!, out Container? container))
        {
            return container;
        }

        if (name.ContainerId == 7)
        {
            foreach ((int _, Container candidate) in _openContainers)
            {
                if (!ReferenceEquals(candidate, inventory.Container))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    public bool TryResolveContainerSlot(FullContainerName name, int slot, out Container? container, out int resolvedSlot)
    {
        container = GetContainer(name);
        resolvedSlot = slot;
        if (container is null)
        {
            return false;
        }

        switch (name.ContainerId)
        {
            case 28:
            case 12:
            case 27:
            case 29:
            case 6:
            case 33:
            case 58:
            case 59:
                resolvedSlot = slot;
                break;
        }

        return true;
    }
}
