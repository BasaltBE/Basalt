using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public class DispenserBlockActor : BlockActor {
    protected readonly int facing;
    protected readonly (int X, int Y, int Z) facingOffset;

    public readonly BlockContainer Container;
    public string LootTable = string.Empty;
    public int LootTableSeed;
    public override Container? Inventory => Container;

    public DispenserBlockActor(BlockActorContext context) : this(context, ContainerType.Dispenser, "Dispenser") { }

    protected DispenserBlockActor(BlockActorContext context, ContainerType containerType, string actorIdentifier) : base(context) {
        Container = new BlockContainer(
            context.Position,
            9,
            ContainerId.First,
            ContainerEnumName.LevelEntity,
            containerType
        );
        Container.Changed += MarkDirty;
        ActorIdentifier = actorIdentifier;
        if (context.Permutation.State.Values.GetValueOrDefault("facing_direction") is int value &&
            (uint)value < BlockDirections.ByFace.Length) {
            facing = value;
            facingOffset = BlockDirections.OffsetByDirection[BlockDirections.ByFace[value]];
        }
        else {
            facing = -1;
        }
    }

    public override string ActorIdentifier { get; }

    public override void OnRead(CompoundTag nbt) {
        LootTable = nbt.Get<StringTag>("LootTable")?.Value ?? string.Empty;
        LootTableSeed = nbt.Get<IntTag>("LootTableSeed")?.Value ?? 0;
        ReadItems(nbt, Container);
    }

    public override void OnWrite(CompoundTag nbt) {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("Items", WriteItems(Container, includePickupState: true));
        if (LootTable.Length > 0) {
            nbt.Add("LootTable", new StringTag { Value = LootTable });
            nbt.Add("LootTableSeed", new IntTag { Value = LootTableSeed });
        }
        else {
            nbt.Remove("LootTable");
            nbt.Remove("LootTableSeed");
        }
    }

    public override void OnPlace(ItemStack? item, byte face) {
        CompoundTag? blockEntity = item?.Nbt?.Get<CompoundTag>("BlockEntityTag") ?? item?.Nbt;
        if (blockEntity is not null)
            OnRead(blockEntity);
    }

    public override void OnInteract(Player player) => Container.Open(player);

    public override void OnBreak(Player player) {
        for (int slot = 0; slot < Container.ContainerSize; slot++) {
            if (Container.TakeItem(slot, ushort.MaxValue) is not { } item)
                continue;

            new ItemEntity(item, player, Position.X, Position.Y, Position.Z).Spawn(Dimension);
        }
    }

    public override void OnRemove() => Container.CloseViewers();

    internal void Activate() {
        int selectedSlot = -1;
        int occupiedSlots = 0;
        for (int slot = 0; slot < Container.ContainerSize; slot++) {
            if (Container.GetItem(slot) is null)
                continue;

            occupiedSlots++;
            if (Random.Shared.Next(occupiedSlots) == 0)
                selectedSlot = slot;
        }

        Vec3 position = new() { X = Position.X + 0.5f, Y = Position.Y + 0.5f, Z = Position.Z + 0.5f };
        if (selectedSlot < 0 || facing < 0 || !Dispense(selectedSlot)) {
            Dimension.Broadcast(new LevelEventPacket {
                EventId = 1001,
                Position = position
            }, Position.X >> 4, Position.Z >> 4);
            return;
        }

        Dimension.Broadcast(new LevelEventPacket {
            EventId = 1000,
            Position = position
        }, Position.X >> 4, Position.Z >> 4, additionalPacket: new LevelEventPacket {
            EventId = 2000,
            Position = position,
            Data = facing
        });
    }

    protected virtual bool Dispense(int slot) {
        if (Container.GetItem(slot) is not { } stored || facing < 0)
            return false;

        if (this is not DropperBlockActor &&
            stored.Type.Identifier is "minecraft:flint_and_steel" or "minecraft:fire_charge") {
            int targetX = Position.X + facingOffset.X;
            int targetY = Position.Y + facingOffset.Y;
            int targetZ = Position.Z + facingOffset.Z;
            if (TntTrait.Prime(Dimension, targetX, targetY, targetZ)) {
                if (stored.Type.Identifier == "minecraft:fire_charge")
                    Container.TakeItem(slot);
                else
                    stored.DamageDurability();
                return true;
            }
        }

        if (stored.Type.PropertiesPayload.EntityPlacer?.GetValueOrDefault("entity") is { } entityIdentifier &&
            EntityPalette.Find(entityIdentifier) is { } entityType) {
            int targetX = Position.X + facingOffset.X;
            int targetY = Position.Y + facingOffset.Y;
            int targetZ = Position.Z + facingOffset.Z;
            if (Dimension.GetLoadedPermutation(targetX, targetY, targetZ) is not { } target ||
                BlockPalette.FindType(target.Identifier)?.Solid == true)
                return false;

            Entity entity = new(entityType);
            entity.Position.X = targetX + 0.5f;
            entity.Position.Y = targetY;
            entity.Position.Z = targetZ + 0.5f;
            entity.Spawn(Dimension);
            Container.TakeItem(slot, 1);
            return true;
        }

        ItemStack? dispensed = Container.TakeItem(slot, 1);
        if (dispensed is null)
            return false;

        ItemEntity itemEntity = new(dispensed, Position.X, Position.Y, Position.Z);
        itemEntity.Position.X = Position.X + 0.5f + facingOffset.X * 0.7f;
        itemEntity.Position.Y = Position.Y + 0.5f + facingOffset.Y * 0.7f - (facing is 0 or 1 ? 0.125f : 0.15625f);
        itemEntity.Position.Z = Position.Z + 0.5f + facingOffset.Z * 0.7f;
        if (itemEntity.GetTrait<EntityMovementTrait>() is { } movement) {
            movement.Motion.X = facingOffset.X * 0.2f + (Random.Shared.NextSingle() - 0.5f) * 0.1f;
            movement.Motion.Y = 0.2f + (Random.Shared.NextSingle() - 0.5f) * 0.2f;
            movement.Motion.Z = facingOffset.Z * 0.2f + (Random.Shared.NextSingle() - 0.5f) * 0.1f;
        }
        itemEntity.Spawn(Dimension);
        return true;
    }
}
