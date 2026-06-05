namespace Basalt.Core.Entities;

using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Player = Basalt.Core.Player.Player;

public sealed class ItemEntity : Entity
{
    public ItemStack Item { get; }
    private ulong _nextMergeTick;
    public ulong MergeLockedUntilTick { get; private set; }
    public ulong PickupLockedUntilTick { get; private set; }

    public ItemEntity(ItemStack item) : base("minecraft:item")
    {
        Item = item;
    }

    public void LockMergeUntil(ulong tick)
    {
        if (tick > MergeLockedUntilTick)
        {
            MergeLockedUntilTick = tick;
        }
    }

    public void LockPickupUntil(ulong tick)
    {
        if (tick > PickupLockedUntilTick)
        {
            PickupLockedUntilTick = tick;
        }
    }

    public override void Spawn(Basalt.Core.Worlds.Dimensions.Dimension dimension, EntitySpawnOptions options)
    {
        base.Spawn(dimension, options);
        Dimension?.Broadcast(CreateAddItemActorPacket());
    }

    public override void SpawnTo(Player player, ulong tick)
    {
        player.Send(CreateAddItemActorPacket());
    }

    private AddItemActorPacket CreateAddItemActorPacket()
    {
        LegacyItem stack = Item.ToNetworkStack();
        return new AddItemActorPacket
        {
            EntityUniqueId = UniqueId,
            EntityRuntimeId = RuntimeId,
            Item = new ItemInstance
            {
                Stack = stack,
                StackNetworkId = stack.ItemStackId ?? 0
            },
            Position = Position,
            Velocity = Velocity,
            EntityMetadata = CreateActorDataPacket(Dimension?.World is Basalt.Core.Worlds.Tickable tickable ? tickable.TickValue : 0).Metadata,
            FromFishing = false
        };
    }

    public void TryMergeNearby(ulong currentTick)
    {
        if (Dimension is null || PendingDespawn || !IsAlive || currentTick < _nextMergeTick || Item.StackSize == 0 || currentTick < MergeLockedUntilTick)
        {
            return;
        }

        _nextMergeTick = currentTick + 15;
        int maxStack = Item.Type.MaxStackSize;
        if (Item.StackSize >= maxStack)
        {
            return;
        }

        bool merged = false;
        const float mergeRadiusSquared = 1.5f * 1.5f;

        foreach (Entity entity in Dimension.Entities)
        {
            if (entity is not ItemEntity other || ReferenceEquals(other, this) || !other.IsAlive || other.PendingDespawn)
            {
                continue;
            }

            if (currentTick < other.MergeLockedUntilTick)
            {
                continue;
            }

            if (!IsGrounded(other.Position))
            {
                continue;
            }

            float dx = other.Position.X - Position.X;
            float dy = other.Position.Y - Position.Y;
            float dz = other.Position.Z - Position.Z;
            if ((dx * dx) + (dy * dy) + (dz * dz) > mergeRadiusSquared)
            {
                continue;
            }

            if (!CanMergeWith(other))
            {
                continue;
            }

            int space = maxStack - Item.StackSize;
            if (space <= 0)
            {
                break;
            }

            int moved = Math.Min(space, other.Item.StackSize);
            if (moved <= 0)
            {
                continue;
            }

            Item.SetStackSize((ushort)(Item.StackSize + moved));
            other.Item.SetStackSize((ushort)(other.Item.StackSize - moved));
            merged = true;

            if (other.Item.StackSize == 0)
            {
                other.Despawn(new EntityDespawnOptions());
            }
            else
            {
                other.Resend();
            }
        }

        if (merged)
        {
            Resend();
        }
    }

    public void TryPickupNearby(ulong currentTick)
    {
        if (Dimension is null || PendingDespawn || !IsAlive || Item.StackSize == 0 || currentTick < PickupLockedUntilTick)
        {
            return;
        }

        if (Dimension.World?.Server is not Basalt.Core.Server server)
        {
            return;
        }

        const float pickupRadiusSquared = 1.5f * 1.5f;

        foreach ((_, var player) in server.Players)
        {
            if (player.Dimension != Dimension || !player.IsAlive || !player.Spawned)
            {
                continue;
            }

            float dx = player.Position.X - Position.X;
            float dz = player.Position.Z - Position.Z;
            if ((dx * dx) + (dz * dz) > pickupRadiusSquared)
            {
                continue;
            }

            ushort moved = player.CollectItem(Item);
            if (moved == 0)
            {
                continue;
            }

            ushort after = Item.StackSize;

            Dimension.Broadcast(new TakeItemActorPacket
            {
                ItemEntityRuntimeId = RuntimeId,
                TakerEntityRuntimeId = player.RuntimeId
            });

            if (after == 0)
            {
                Despawn(new EntityDespawnOptions());
                return;
            }

            Resend();
            return;
        }
    }

    private bool CanMergeWith(ItemEntity other)
    {
        if (Item.Type != other.Item.Type || Item.Metadata != other.Item.Metadata || !Item.CanStackWith(other.Item))
        {
            return false;
        }

        string thisNbt = Item.ExtraData?.Nbt?.ToString() ?? string.Empty;
        string otherNbt = other.Item.ExtraData?.Nbt?.ToString() ?? string.Empty;
        return string.Equals(thisNbt, otherNbt, StringComparison.Ordinal);
    }

    private bool IsGrounded(Vec3f position)
    {
        if (Dimension is null)
        {
            return false;
        }

        string identifier = Dimension.GetPermutation(
            (int)MathF.Floor(position.X),
            (int)MathF.Floor(position.Y - 0.001f),
            (int)MathF.Floor(position.Z)
        ).Type.Identifier;

        if (string.Equals(identifier, "minecraft:air", StringComparison.Ordinal))
        {
            return false;
        }

        if (identifier.Contains("water", StringComparison.Ordinal) || identifier.Contains("lava", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private void Resend()
    {
        if (Dimension is null || PendingDespawn || !IsAlive)
        {
            return;
        }

        Dimension.Broadcast(new RemoveActorPacket
        {
            EntityUniqueId = UniqueId
        });
        Dimension.Broadcast(CreateAddItemActorPacket());
    }

    public override void OnPhysicsTick(ulong currentTick, bool grounded)
    {
        TryPickupNearby(currentTick);
        if (grounded)
        {
            TryMergeNearby(currentTick);
        }
    }
}
