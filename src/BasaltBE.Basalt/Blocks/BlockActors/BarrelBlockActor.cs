using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class BarrelBlockActor : BlockActor
{
    public readonly BlockContainer Container;
    public bool Findable;
    public string LootTable = string.Empty;
    public int LootTableSeed;
    public override string ActorIdentifier => "Barrel";
    public override Container? Inventory => Container;

    public BarrelBlockActor(BlockActorContext context) : base(context)
    {
        Container = new BlockContainer(context.Position, 27, containerName: ContainerEnumName.Barrel);
        Container.Changed += MarkDirty;
        Container.FirstViewerOpened += OnFirstViewerOpened;
        Container.LastViewerClosed += OnLastViewerClosed;
    }

    public override void OnRead(CompoundTag nbt)
    {
        Findable = nbt.Get<ByteTag>("Findable")?.Value == 1;
        LootTable = nbt.Get<StringTag>("LootTable")?.Value ?? string.Empty;
        LootTableSeed = nbt.Get<IntTag>("LootTableSeed")?.Value ?? 0;
        ReadItems(nbt, Container);
    }

    public override void OnWrite(CompoundTag nbt)
    {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("Findable", new ByteTag { Value = Findable ? (byte)1 : (byte)0 });
        nbt.Add("Items", WriteItems(Container));
        if (LootTable.Length > 0)
        {
            nbt.Add("LootTable", new StringTag { Value = LootTable });
            nbt.Add("LootTableSeed", new IntTag { Value = LootTableSeed });
        }
        else
        {
            nbt.Remove("LootTable");
            nbt.Remove("LootTableSeed");
        }
    }

    public override void OnInteract(Player player) => Container.Open(player);

    public override void OnPlace(ItemStack? item, byte face)
    {
        CompoundTag? blockEntity = item?.Nbt?.Get<CompoundTag>("BlockEntityTag") ?? item?.Nbt;
        if (blockEntity is not null)
            OnRead(blockEntity);
    }

    public override void OnBreak(Player player)
    {
        for (int slot = 0; slot < Container.ContainerSize; slot++)
        {
            if (Container.GetItem(slot) is not { } item)
                continue;

            ItemEntity drop = new(item.Clone(), player, Position.X, Position.Y, Position.Z);
            drop.Spawn(Dimension);
        }
    }

    public override void OnRemove() => Container.CloseViewers();

    void OnFirstViewerOpened() => SetOpen(true);

    void OnLastViewerClosed() => SetOpen(false);

    void SetOpen(bool open)
    {
        BlockPermutation current = Dimension.GetPermutation(Position.X, Position.Y, Position.Z);
        if (current.State.Values.GetValueOrDefault("open_bit") is bool currentOpen && currentOpen == open)
            return;

        BlockPermutation permutation = BlockPalette.Find(current.Identifier, current.State, "open_bit", open) ?? current;
        if (permutation == current)
            return;

        if (Dimension.GetChunk(Position.X >> 4, Position.Z >> 4) is not { } chunk)
            return;

        chunk.SetPermutation(Position.X & 15, Position.Y, Position.Z & 15, permutation);
        Dimension.Broadcast(new UpdateBlockPacket
        {
            Position = Position,
            BlockRuntimeId = unchecked((uint)permutation.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, Position.X >> 4, Position.Z >> 4, additionalPacket: new LevelSoundEventPacket
        {
            SoundEvent = open ? "barrel.open" : "barrel.close",
            Position = new Vec3 { X = Position.X + 0.5f, Y = Position.Y + 0.5f, Z = Position.Z + 0.5f }
        });
    }
}
