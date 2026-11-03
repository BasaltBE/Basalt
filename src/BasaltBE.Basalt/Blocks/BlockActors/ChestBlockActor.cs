using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class ChestBlockActor : BlockActor {
    public BlockContainer Container;
    public string CustomName = string.Empty;
    public string LootTable = string.Empty;
    public int LootTableSeed;
    public bool Findable;
    public BlockPos? PairPosition;
    public bool PairLead;
    public override string ActorIdentifier => "Chest";
    public override Container? Inventory => Container;

    public ChestBlockActor(BlockActorContext context) : base(context) {
        Container = CreateContainer(context.Position, 27);
    }

    BlockContainer CreateContainer(BlockPos position, int size) {
        BlockContainer container = new(position, size);
        container.FirstViewerOpened += OnFirstViewerOpened;
        container.LastViewerClosed += OnLastViewerClosed;
        container.Changed += MarkDirty;
        return container;
    }

    public override void OnRead(CompoundTag nbt) {
        if (nbt.TryGet("CustomName", out StringTag? name))
            CustomName = name!.Value;
        if (nbt.TryGet("LootTable", out StringTag? lootTable))
            LootTable = lootTable!.Value;
        if (nbt.TryGet("LootTableSeed", out IntTag? seed))
            LootTableSeed = seed!.Value;
        if (nbt.TryGet("Findable", out ByteTag? findable))
            Findable = findable!.Value != 0;
        if (nbt.TryGet("pairx", out IntTag? pairX) && nbt.TryGet("pairz", out IntTag? pairZ)) {
            PairPosition = new BlockPos { X = pairX!.Value, Y = Position.Y, Z = pairZ!.Value };
            PairLead = nbt.Get<ByteTag>("pairlead")?.Value == 1;
        }

        ReadItems(nbt, Container);
    }

    public override void OnWrite(CompoundTag nbt) {
        if (CustomName.Length > 0)
            nbt.Add("CustomName", new StringTag { Value = CustomName });
        else
            nbt.Remove("CustomName");

        if (LootTable.Length > 0) {
            nbt.Add("LootTable", new StringTag { Value = LootTable });
            nbt.Add("LootTableSeed", new IntTag { Value = LootTableSeed });
        }
        else {
            nbt.Remove("LootTable");
            nbt.Remove("LootTableSeed");
        }
        nbt.Add("Findable", new ByteTag { Value = Findable ? (byte)1 : (byte)0 });

        if (PairPosition is { } pair) {
            nbt.Add("pairx", new IntTag { Value = pair.X });
            nbt.Add("pairz", new IntTag { Value = pair.Z });
            nbt.Add("pairlead", new ByteTag { Value = PairLead ? (byte)1 : (byte)0 });
        }
        else {
            nbt.Remove("pairx");
            nbt.Remove("pairz");
            nbt.Remove("pairlead");
        }

        int firstSlot = PairPosition is not null && !PairLead ? 27 : 0;
        ListTag items = new() { ElementType = TagType.Compound };
        for (int slot = 0; slot < 27; slot++) {
            if (Container.GetItem(firstSlot + slot) is not { } stack)
                continue;

            CompoundTag item = new();
            item.Add("Name", new StringTag { Value = stack.Type.Identifier });
            item.Add("Count", new ByteTag { Value = checked((byte)stack.StackSize) });
            item.Add("Damage", new ShortTag { Value = unchecked((short)stack.Metadata) });
            item.Add("Slot", new ByteTag { Value = checked((byte)slot) });
            if (stack.Nbt is { } itemNbt)
                item.Add("tag", itemNbt);
            items.Value.Add(item);
        }
        nbt.Add("Items", items);
    }

    public override void OnPlace(ItemStack? item, byte face) {
        CompoundTag? blockEntity = item?.Nbt?.Get<CompoundTag>("BlockEntityTag") ?? item?.Nbt;
        if (blockEntity is not null)
            OnRead(blockEntity);
        PairWithNeighbor();
    }

    public override CompoundTag CreateItemData() {
        CompoundTag itemData = base.CreateItemData();
        itemData.Remove("pairx");
        itemData.Remove("pairz");
        itemData.Remove("pairlead");
        return itemData;
    }

    public override void OnInteract(Player player) {
        AttachPairedContainer();
        Container.Open(player);
    }

    public override void OnBreak(Player player) {
        int firstSlot = PairPosition is not null && !PairLead ? 27 : 0;
        for (int slot = firstSlot; slot < firstSlot + 27; slot++) {
            if (Container.GetItem(slot) is not { } item)
                continue;

            ItemEntity drop = new(item.Clone(), player, Position.X, Position.Y, Position.Z);
            drop.Spawn(Dimension);
        }
    }

    void OnFirstViewerOpened() => SendOpenState(true);

    void OnLastViewerClosed() => SendOpenState(false);

    void SendOpenState(bool open) {
        BlockEventPacket packet = new() {
            Position = Position,
            EventType = BlockEventType.ChangeChestState,
            EventData = open ? 1 : 0
        };
        LevelSoundEventPacket sound = new() {
            SoundEvent = open ? "chest.open" : "chest.closed",
            Position = new Vec3 { X = Position.X + 0.5f, Y = Position.Y + 0.5f, Z = Position.Z + 0.5f }
        };
        Dimension.Broadcast(packet, Position.X >> 4, Position.Z >> 4, additionalPacket: sound);

        if (PairPosition is not { } pair)
            return;

        Dimension.Broadcast(new BlockEventPacket {
            Position = pair,
            EventType = BlockEventType.ChangeChestState,
            EventData = open ? 1 : 0
        }, pair.X >> 4, pair.Z >> 4);
    }

    public override void OnRemove() {
        Container.CloseViewers();
        if (PairPosition is not { } pairPosition ||
            Dimension.GetChunk(pairPosition.X >> 4, pairPosition.Z >> 4) is not { } chunk ||
            Dimension.GetBlockActor(chunk, pairPosition.X & 15, pairPosition.Y, pairPosition.Z & 15) is not ChestBlockActor pair)
            return;

        pair.Container.CloseViewers();
        int pairSlotOffset = pair.PairLead ? 0 : 27;
        pair.PairPosition = null;
        pair.PairLead = false;
        pair.Container = CopyHalf(pair, pair.Container, pairSlotOffset);
        PairPosition = null;
        PairLead = false;
        pair.Update();
    }

    void PairWithNeighbor() {
        bool alongX = Permutation.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is "north" or "south";
        for (int side = 0; side < 2; side++) {
            int x = Position.X + (alongX ? (side == 0 ? -1 : 1) : 0);
            int z = Position.Z + (alongX ? 0 : (side == 0 ? -1 : 1));
            if (Dimension.GetChunk(x >> 4, z >> 4) is not { } chunk ||
                chunk.GetPermutation(x & 15, Position.Y, z & 15).Identifier != "minecraft:chest" ||
                Dimension.GetBlockActor(chunk, x & 15, Position.Y, z & 15) is not ChestBlockActor pair ||
                pair.PairPosition is not null ||
                !Equals(pair.Permutation.State.Values.GetValueOrDefault("minecraft:cardinal_direction"),
                    Permutation.State.Values.GetValueOrDefault("minecraft:cardinal_direction")))
                continue;

            PairPosition = pair.Position;
            pair.PairPosition = Position;
            PairLead = Position.X < pair.Position.X || Position.X == pair.Position.X && Position.Z < pair.Position.Z;
            pair.PairLead = !PairLead;
            pair.Container.CloseViewers();
            Container.CloseViewers();
            BlockPos leadPosition = PairLead ? Position : pair.Position;
            BlockContainer shared = CreateContainer(leadPosition, 54);
            CopyItems(Container, shared, PairLead ? 0 : 27);
            CopyItems(pair.Container, shared, PairLead ? 27 : 0);
            Container = shared;
            pair.Container = shared;
            pair.Update();
            return;
        }
    }

    void AttachPairedContainer() {
        if (PairPosition is not { } pairPosition ||
            Dimension.GetChunk(pairPosition.X >> 4, pairPosition.Z >> 4) is not { } chunk ||
            Dimension.GetBlockActor(chunk, pairPosition.X & 15, pairPosition.Y, pairPosition.Z & 15) is not ChestBlockActor pair)
            return;

        if (ReferenceEquals(Container, pair.Container))
            return;

        if (pair.PairPosition is { } existingPair &&
            (existingPair.X != Position.X || existingPair.Y != Position.Y || existingPair.Z != Position.Z))
            return;
        pair.PairPosition ??= Position;
        pair.PairLead = !PairLead;
        BlockPos leadPosition = PairLead ? Position : pair.Position;
        BlockContainer shared = CreateContainer(leadPosition, 54);
        CopyItems(Container, shared, PairLead ? 0 : 27);
        CopyItems(pair.Container, shared, PairLead ? 27 : 0);
        Container = shared;
        pair.Container = shared;
    }

    static void CopyItems(Container source, Container destination, int offset) {
        for (int slot = 0; slot < 27; slot++) {
            if (source.GetItem(slot) is { } item)
                destination.LoadItem(offset + slot, item.Clone());
        }
    }

    static BlockContainer CopyHalf(ChestBlockActor actor, Container source, int offset) {
        BlockContainer result = actor.CreateContainer(actor.Position, 27);
        for (int slot = 0; slot < 27; slot++) {
            if (source.GetItem(offset + slot) is { } item)
                result.LoadItem(slot, item.Clone());
        }
        return result;
    }

}
