using System.Collections.Frozen;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class ShulkerBoxBlockActor : BlockActor {
    private static readonly FrozenSet<ItemType> BannedItems = ItemPalette.Types
        .Where(static type => type.Identifier.EndsWith("_shulker_box", StringComparison.Ordinal))
        .ToFrozenSet();

    public readonly BlockContainer Container;
    public byte Facing;
    public bool Findable;
    public string CustomName = string.Empty;
    public override string ActorIdentifier => "ShulkerBox";
    public override bool DropsInCreative => true;

    public ShulkerBoxBlockActor(BlockActorContext context) : base(context) {
        Container = new BlockContainer(
            context.Position,
            27,
            containerName: ContainerEnumName.ShulkerBox,
            bannedItems: BannedItems
        );
        Container.FirstViewerOpened += OnFirstViewerOpened;
        Container.LastViewerClosed += OnLastViewerClosed;
    }

    public override void OnRead(CompoundTag nbt) {
        Facing = nbt.Get<ByteTag>("facing")?.Value ?? 1;
        Findable = nbt.Get<ByteTag>("Findable")?.Value == 1;
        CustomName = nbt.Get<StringTag>("CustomName")?.Value ?? string.Empty;
        ReadItems(nbt, Container);
    }

    public override void OnWrite(CompoundTag nbt) {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("facing", new ByteTag { Value = Facing });
        nbt.Add("Findable", new ByteTag { Value = Findable ? (byte)1 : (byte)0 });
        nbt.Add("Items", WriteItems(Container));
        if (CustomName.Length > 0)
            nbt.Add("CustomName", new StringTag { Value = CustomName });
        else
            nbt.Remove("CustomName");
    }

    public override void OnPlace(ItemStack? item, byte face) {
        Facing = face;
        CompoundTag? blockEntity = item?.Nbt?.Get<CompoundTag>("BlockEntityTag") ?? item?.Nbt;
        if (blockEntity is null)
            return;

        CustomName = blockEntity.Get<StringTag>("CustomName")?.Value ?? string.Empty;
        ReadItems(blockEntity, Container);
    }

    public override void OnInteract(Player player) => Container.Open(player);

    public override void OnRemove() => Container.CloseViewers();

    public override ItemStack? CreateDrop() {
        ItemType? type = ItemPalette.Find(Identifier);
        if (type is null || type.NetworkId == 0)
            return null;

        ItemStack item = new(type);
        if (Container.GetItem(0) is null && CustomName.Length == 0) {
            bool empty = true;
            for (int slot = 1; slot < Container.ContainerSize; slot++) {
                if (Container.GetItem(slot) is not null) {
                    empty = false;
                    break;
                }
            }
            if (empty)
                return item;
        }

        CompoundTag blockEntity = new();
        blockEntity.Add("Items", WriteItems(Container));
        if (CustomName.Length > 0)
            blockEntity.Add("CustomName", new StringTag { Value = CustomName });
        item.Nbt = new CompoundTag();
        item.Nbt.Add("BlockEntityTag", blockEntity);
        return item;
    }

    void OnFirstViewerOpened() => SendOpenState(true);

    void OnLastViewerClosed() => SendOpenState(false);

    void SendOpenState(bool open) {
        Dimension.Broadcast(new BlockEventPacket {
            Position = Position,
            EventType = BlockEventType.ChangeChestState,
            EventData = open ? 1 : 0
        }, Position.X >> 4, Position.Z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = open ? "shulkerbox.open" : "shulkerbox.closed",
            Position = new Vec3 { X = Position.X + 0.5f, Y = Position.Y + 0.5f, Z = Position.Z + 0.5f }
        });
    }
}
