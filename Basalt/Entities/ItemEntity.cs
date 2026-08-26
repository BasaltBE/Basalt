namespace Basalt.Core.Entities;

using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Player = Basalt.Core.Player.Player;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class ItemEntity : Entity {
    public ItemStack Item { get; }
    private ulong _nextMergeTick;
    private ulong _nextPickupLogTick;
    public ulong MergeLockedUntilTick { get; private set; }
    public ulong PickupLockedUntilTick { get; private set; }

    public ItemEntity(ItemStack item) : base("minecraft:item") {
        Item = item;
    }

    public void LockMergeUntil(ulong tick) {
        if (tick > MergeLockedUntilTick) {
            MergeLockedUntilTick = tick;
        }
    }

    public void LockPickupUntil(ulong tick) {
        if (tick > PickupLockedUntilTick) {
            PickupLockedUntilTick = tick;
        }
    }

    public override void Spawn(Basalt.Core.Worlds.Dimensions.Dimension dimension, EntitySpawnOptions options) {
        base.Spawn(dimension, options);
    }

    public override void SpawnTo(Player player, ulong tick, Vec3? position = null) {
        player.Send(CreateAddItemActorPacket());
    }

    public override CompoundTag Write() {
        CompoundTag tag = base.Write();
        tag.Set("item", Item.Serialize());
        return tag;
    }

    private AddItemActorPacket CreateAddItemActorPacket() {
        return new AddItemActorPacket {
            ActorUniqueId = UniqueId ,
            ActorRuntimeId = RuntimeId ,
            Item = Item.ToNetworkStackDescriptor(),
            Position = Position,
            Velocity = Velocity,
            EntityData = CreateActorDataPacket(Dimension?.World is Basalt.Core.Worlds.Tickable tickable ? tickable.TickValue : 0).ActorData,
            FromFishing = false,
        };
    }

    public void TryMergeNearby(ulong currentTick) {
        if (Dimension is null || PendingDespawn || !IsAlive || currentTick < _nextMergeTick || Item.StackSize == 0 || currentTick < MergeLockedUntilTick) {
            return;
        }

        _nextMergeTick = currentTick + 15;
        int maxStack = Item.Type.MaxStackSize;
        if (Item.StackSize >= maxStack) {
            return;
        }

        bool merged = false;
        const float mergeRadiusSquared = 1.5f * 1.5f;

        foreach (Entity entity in Dimension.Entities) {
            if (entity is not ItemEntity other || ReferenceEquals(other, this) || !other.IsAlive || other.PendingDespawn) {
                continue;
            }

            if (currentTick < other.MergeLockedUntilTick) {
                continue;
            }

            if (!IsGrounded(other.Position)) {
                continue;
            }

            float dx = other.Position.X - Position.X;
            float dy = other.Position.Y - Position.Y;
            float dz = other.Position.Z - Position.Z;
            if ((dx * dx) + (dy * dy) + (dz * dz) > mergeRadiusSquared) {
                continue;
            }

            if (!CanMergeWith(other)) {
                continue;
            }

            int space = maxStack - Item.StackSize;
            if (space <= 0) {
                break;
            }

            int moved = Math.Min(space, other.Item.StackSize);
            if (moved <= 0) {
                continue;
            }

            Item.SetStackSize((ushort)(Item.StackSize + moved));
            other.Item.SetStackSize((ushort)(other.Item.StackSize - moved));
            merged = true;

            if (other.Item.StackSize == 0) {
                other.Despawn(new EntityDespawnOptions());
            }
            else {
                other.Resend();
            }
        }

        if (merged) {
            Resend();
        }
    }

    public void TryPickupNearby(ulong currentTick) {
        if (Dimension is null || PendingDespawn || !IsAlive || Item.StackSize == 0 || currentTick < PickupLockedUntilTick) {
            return;
        }

        if (Dimension.World?.Server is not Basalt.Core.Server server) {
            return;
        }

        const float pickupRadius = 2.5f;
        const float pickupRadiusSquared = pickupRadius * pickupRadius;
        const float pickupVerticalTolerance = 2f;

        foreach (Player player in Dimension.GetPlayers()) {
            if (!player.IsAlive || !player.Spawned) {
                continue;
            }

            float playerHeight = player.GetTrait<EntityCollisionTrait>()?.Height ?? EntityCollisionTrait.DefaultHeight;
            Vec3 eyePos = player.GetEyePosition();
            Vec3 feetPos = new() {
                X = eyePos.X,
                Y = eyePos.Y - playerHeight,
                Z = eyePos.Z
            };
            float dx = feetPos.X - Position.X;
            float dy = feetPos.Y - Position.Y;
            float dz = feetPos.Z - Position.Z;
            if (MathF.Abs(dy) > pickupVerticalTolerance || (dx * dx) + (dz * dz) > pickupRadiusSquared) {
                continue;
            }

            var signal = new Basalt.Core.Events.PlayerItemPickupSignal(player, Item, this);
            server.Emit(signal);
            if (!signal.Emit()) {
                continue;
            }

            ushort moved = player.CollectItem(Item);
            if (moved == 0) {
                if (currentTick >= _nextPickupLogTick) {
                    _nextPickupLogTick = currentTick + 20;
                    Logger.Warn("Item pickup rejected player:{0} item:{1} count:{2} inventoryFullOrMismatch:true", player.Username, Item.Identifier, Item.StackSize);
                }
                continue;
            }

            ushort after = Item.StackSize;

            Dimension.Broadcast(new TakeItemActorPacket {
                ItemRuntimeId = RuntimeId ,
                ActorRuntimeId = player.RuntimeId 
            });

            // Logger.Info("Item picked up player:{0} item:{1} count:{2} moved:{3}", player.Username, Item.Identifier, after, moved);

            if (after == 0) {
                Despawn(new EntityDespawnOptions());
                return;
            }

            Resend();
            return;
        }
    }

    private bool CanMergeWith(ItemEntity other) {
        if (Item.Type != other.Item.Type || Item.Metadata != other.Item.Metadata || !Item.CanStackWith(other.Item)) {
            return false;
        }

        string thisNbt = Item.Storage?.ToString() ?? string.Empty;
        string otherNbt = other.Item.Storage?.ToString() ?? string.Empty;
        return string.Equals(thisNbt, otherNbt, StringComparison.Ordinal);
    }

    private bool IsGrounded(Vec3 position) {
        if (Dimension is null) {
            return false;
        }

        if (!Dimension.TryGetLoadedPermutation(
                (int)MathF.Floor(position.X),
                (int)MathF.Floor(position.Y - 0.001f),
                (int)MathF.Floor(position.Z),
                out BlockPermutation? permutation) ||
            permutation is null) {
            return false;
        }

        string identifier = permutation.Type.Identifier;

        if (string.Equals(identifier, "minecraft:air", StringComparison.Ordinal)) {
            return false;
        }

        if (identifier.Contains("water", StringComparison.Ordinal) || identifier.Contains("lava", StringComparison.Ordinal)) {
            return false;
        }

        return true;
    }

    private void Resend() {
        if (Dimension is null || PendingDespawn || !IsAlive) {
            return;
        }

        Dimension.Broadcast(new RemoveActorPacket {
            ActorUniqueId = UniqueId 
        });
        Dimension.Broadcast(CreateAddItemActorPacket());
    }

    public override void OnPhysicsTick(ulong currentTick, bool grounded) {
        TryPickupNearby(currentTick);
        if (grounded) {
            TryMergeNearby(currentTick);
        }
    }
}
