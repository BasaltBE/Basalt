using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities;

public sealed class ItemEntity : Entity
{
    private const ulong MergeDelayTicks = 40;
    private const ulong PickupDelayTicks = 40;
    private const ulong MergeCheckInterval = 15;
    private const ulong DespawnDelayTicks = 6000;
    private const float MergeRadiusSquared = 2.25f;
    private const float PickupRadiusSquared = 6.25f;
    private const float PickupVerticalTolerance = 2f;
    private static readonly EntityType ItemType = new() { Identifier = "minecraft:item" };
    private readonly EntityMovementTrait movement;
    private ulong mergeUnlockTick;
    private ulong pickupUnlockTick;
    private ulong despawnTick;
    private ulong nextMergeTick;

    public readonly ItemStack Item;

    public ItemEntity(ItemStack item, Player player, bool randomly)
        : base(ItemType)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(player);
        if (item.StackSize == 0 || item.Type.NetworkId == 0)
            throw new ArgumentException("Dropped items must have a valid non-empty stack.", nameof(item));
        if (item.Container is not null)
            throw new ArgumentException("Dropped items cannot belong to a container.", nameof(item));

        Item = item;
        movement = new(this)
        {
            Gravity = 0.04f,
            Drag = 0.02f,
            VerticalDragBeforeGravity = true
        };
        AddTrait(movement);
        mergeUnlockTick = player.World.TickValue + MergeDelayTicks;
        pickupUnlockTick = player.World.TickValue + PickupDelayTicks;
        despawnTick = player.World.TickValue + DespawnDelayTicks;
        nextMergeTick = mergeUnlockTick;

        float pitch = player.Rotation.X * (MathF.PI / 180);
        float yaw = player.Rotation.Y * (MathF.PI / 180);
        float pitchCosine = MathF.Cos(pitch);
        float directionX = -MathF.Sin(yaw) * pitchCosine;
        float directionY = -MathF.Sin(pitch);
        float directionZ = MathF.Cos(yaw) * pitchCosine;
        Position.X = player.Position.X + directionX * 0.3f;
        Position.Y = player.Position.Y + 1.3f + directionY * 0.3f;
        Position.Z = player.Position.Z + directionZ * 0.3f;

        if (randomly)
        {
            directionX += (Random.Shared.NextSingle() - 0.5f) * 0.1f;
            directionY += (Random.Shared.NextSingle() - 0.5f) * 0.1f;
            directionZ += (Random.Shared.NextSingle() - 0.5f) * 0.1f;
        }

        movement.Motion.X = directionX * 0.4f;
        movement.Motion.Y = directionY * 0.4f;
        movement.Motion.Z = directionZ * 0.4f;
    }

    internal void TickItem()
    {
        if (Dead || SpawnedDimension is not { } dimension || Item.StackSize == 0)
            return;

        ulong tick = dimension.World?.TickValue ?? 0;
        if (tick >= despawnTick)
        {
            dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
            Despawn();
            return;
        }

        if (tick >= pickupUnlockTick)
        {
            foreach (Player player in dimension.GetPlayers())
            {
                if (player.SpawnedDimension != dimension || !player.Spawned || player.Disposed || player.Dead)
                    continue;

                float dx = player.Position.X - Position.X;
                float dy = player.Position.Y - Position.Y;
                float dz = player.Position.Z - Position.Z;
                if (MathF.Abs(dy) > PickupVerticalTolerance || dx * dx + dz * dz > PickupRadiusSquared)
                    continue;

                using Lock.Scope sync = player.Sync.EnterScope();
                if (player.SpawnedDimension != dimension || !player.Spawned || player.Disposed || player.Dead ||
                    player.GetTrait<PlayerInventoryTrait>() is not { } inventory)
                    continue;

                ushort previousSize = Item.StackSize;
                inventory.Inventory.AddItem(Item);
                if (Item.StackSize == previousSize)
                    continue;

                dimension.Broadcast(new TakeItemActorPacket
                {
                    ItemRuntimeId = RuntimeId,
                    ActorRuntimeId = player.RuntimeId
                }, this);
                if (Item.StackSize == 0)
                {
                    dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
                    Despawn();
                    return;
                }

                UpdateViewers(dimension);
                return;
            }
        }

        if (tick < mergeUnlockTick || tick < nextMergeTick || !movement.Grounded || Item.IsStackFull)
            return;

        nextMergeTick = tick + MergeCheckInterval;
        int itemChunkX = (int)MathF.Floor(Position.X) >> 4;
        int itemChunkZ = (int)MathF.Floor(Position.Z) >> 4;
        for (int z = itemChunkZ - 1; z <= itemChunkZ + 1; z++)
        {
            for (int x = itemChunkX - 1; x <= itemChunkX + 1; x++)
            {
                foreach (Entity entity in dimension.EntityIndex.GetChunkEntities(x, z))
                {
                    if (entity is not ItemEntity other || other.RuntimeId <= RuntimeId || other.Dead ||
                        other.SpawnedDimension != dimension || tick < other.mergeUnlockTick ||
                        !other.movement.Grounded || other.Item.IsStackFull || !Item.CanStackWith(other.Item))
                        continue;

                    float dx = other.Position.X - Position.X;
                    float dy = other.Position.Y - Position.Y;
                    float dz = other.Position.Z - Position.Z;
                    if (dx * dx + dy * dy + dz * dz > MergeRadiusSquared)
                        continue;

                    int space = Item.Type.MaxAmount - Item.StackSize;
                    ushort moved = (ushort)Math.Min(space, other.Item.StackSize);
                    Item.SetStackSize((ushort)(Item.StackSize + moved));
                    other.Item.SetStackSize((ushort)(other.Item.StackSize - moved));
                    despawnTick = Math.Max(despawnTick, other.despawnTick);
                    UpdateViewers(dimension);
                    if (other.Item.StackSize == 0)
                    {
                        dimension.Broadcast(new RemoveActorPacket { ActorId = other.UniqueId }, other);
                        other.Despawn();
                    }
                    else
                    {
                        other.UpdateViewers(dimension);
                    }

                    if (Item.IsStackFull)
                        return;
                }
            }
        }
    }

    private void UpdateViewers(Dimension dimension)
    {
        dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
        dimension.Broadcast(new AddItemActorPacket
        {
            ActorId = UniqueId,
            RuntimeId = RuntimeId,
            Item = Item.ToNetworkStackDescriptor(),
            Position = Position,
            Velocity = movement.Motion,
            ActorData = Flags.CreateSnapshot()
        }, this);
    }
}
