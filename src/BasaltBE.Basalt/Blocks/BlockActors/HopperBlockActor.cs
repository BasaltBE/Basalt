using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class HopperBlockActor : BlockActor
{
    private const int TransferDelay = 8;

    public readonly BlockContainer Container;
    public int TransferCooldown;
    public override string ActorIdentifier => "Hopper";
    public override Container? Inventory => Container;

    public HopperBlockActor(BlockActorContext context) : base(context)
    {
        Container = new BlockContainer(
            context.Position,
            5,
            ContainerId.First,
            ContainerEnumName.LevelEntity,
            ContainerType.Hopper
        );
        Container.Changed += MarkDirty;
    }

    public override void OnRead(CompoundTag nbt)
    {
        TransferCooldown = Math.Max(0, nbt.Get<IntTag>("TransferCooldown")?.Value ?? 0);
        ReadItems(nbt, Container);
    }

    public override void OnWrite(CompoundTag nbt)
    {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("TransferCooldown", new IntTag { Value = TransferCooldown });
        nbt.Add("Items", WriteItems(Container));
    }

    public override void OnPlace(ItemStack? item, byte face)
    {
        CompoundTag? blockEntity = item?.Nbt?.Get<CompoundTag>("BlockEntityTag") ?? item?.Nbt;
        if (blockEntity is not null)
            OnRead(blockEntity);
    }

    public override void OnInteract(Player player) => Container.Open(player);

    public override void OnBreak(Player player)
    {
        for (int slot = 0; slot < Container.ContainerSize; slot++)
        {
            if (Container.TakeItem(slot, ushort.MaxValue) is not { } item)
                continue;

            new ItemEntity(item, player, Position.X, Position.Y, Position.Z).Spawn(Dimension);
        }
    }

    public override void OnRemove() => Container.CloseViewers();

    public override void Tick()
    {
        if (TransferCooldown > 0)
        {
            TransferCooldown--;
            if (TransferCooldown == 0)
                MarkDirty();
        }

        bool? powered = Dimension.Redstone.BlockPowered(Dimension, Position.X, Position.Y, Position.Z);
        if (!powered.HasValue)
            return;
        bool isPowered = powered.Value;

        BlockPermutation current = Dimension.GetPermutation(Position.X, Position.Y, Position.Z);
        if (current.State.Values.GetValueOrDefault("toggle_bit") is bool locked && locked != isPowered &&
            BlockPalette.Find(current.Identifier, current.State, "toggle_bit", isPowered) is { } updated &&
            Dimension.SetPermutation(Position.X, Position.Y, Position.Z, updated))
        {
            Dimension.BroadcastBlockUpdate(Position.X, Position.Y, Position.Z, updated);
        }

        if (isPowered || TransferCooldown > 0)
            return;

        TransferCooldown = TransferDelay;
        MarkDirty();
        if (PullFromAbove() || CollectItemsAbove() || PushForward())
            return;
    }

    bool PullFromAbove()
    {
        if (Dimension.GetChunk(Position.X >> 4, Position.Z >> 4) is not { } chunk ||
            Dimension.GetBlockActor(chunk, Position.X & 15, Position.Y + 1, Position.Z & 15) is not { Inventory: { } source } actor ||
            ReferenceEquals(source, Container))
            return false;

        for (int slot = 0; slot < source.ContainerSize; slot++)
        {
            if (source.GetItem(slot) is not { } item || !actor.CanExtract(slot, item, 0) ||
                !InsertOne(this, item, 1))
                continue;

            source.TakeItem(slot);
            return true;
        }
        return false;
    }

    bool CollectItemsAbove()
    {
        int firstChunkX = Position.X >> 4;
        int lastChunkX = (Position.X + 1) >> 4;
        int firstChunkZ = Position.Z >> 4;
        int lastChunkZ = (Position.Z + 1) >> 4;
        for (int chunkZ = firstChunkZ; chunkZ <= lastChunkZ; chunkZ++)
        {
            for (int chunkX = firstChunkX; chunkX <= lastChunkX; chunkX++)
            {
                if (Dimension.GetChunk(chunkX, chunkZ) is null)
                    continue;

                ReadOnlySpan<Entity> entities = Dimension.EntityIndex.GetChunkEntities(chunkX, chunkZ);
                foreach (Entity entity in entities)
                {
                    if (entity is not ItemEntity item || item.Dead ||
                        item.Position.X < Position.X || item.Position.X >= Position.X + 1 ||
                        item.Position.Y < Position.Y + 0.5f || item.Position.Y >= Position.Y + 2 ||
                        item.Position.Z < Position.Z || item.Position.Z >= Position.Z + 1 ||
                        !InsertOne(this, item.Item, 1))
                        continue;

                    item.TakeItems(1);
                    return true;
                }
            }
        }
        return false;
    }

    bool PushForward()
    {
        if (Permutation.State.Values.GetValueOrDefault("facing_direction") is not int face ||
            (uint)face >= BlockDirections.ByFace.Length ||
            !BlockDirections.OffsetByDirection.TryGetValue(BlockDirections.ByFace[face], out (int X, int Y, int Z) offset))
            return false;

        int targetX = Position.X + offset.X;
        int targetY = Position.Y + offset.Y;
        int targetZ = Position.Z + offset.Z;
        if (Dimension.GetChunk(targetX >> 4, targetZ >> 4) is not { } chunk ||
            Dimension.GetBlockActor(chunk, targetX & 15, targetY, targetZ & 15) is not { Inventory: not null } target)
            return false;

        byte targetFace = BlockDirections.OppositeByFace[face];
        if (ReferenceEquals(target.Inventory, Container))
            return false;
        for (int slot = 0; slot < Container.ContainerSize; slot++)
        {
            if (Container.GetItem(slot) is not { } item || !InsertOne(target, item, targetFace))
                continue;

            Container.TakeItem(slot);
            return true;
        }
        return false;
    }

    static bool InsertOne(BlockActor destination, ItemStack item, byte face)
    {
        if (destination.Inventory is not { } inventory)
            return false;

        for (int slot = 0; slot < inventory.ContainerSize; slot++)
        {
            if (!destination.CanInsert(slot, item, face))
                continue;

            if (inventory.GetItem(slot) is { } existing)
            {
                if (!existing.CanStackWith(item) || existing.StackSize >= existing.Type.MaxAmount)
                    continue;

                existing.IncrementStack();
                return true;
            }

            inventory.SetItem(slot, item.Clone(1));
            return true;
        }
        return false;
    }
}
