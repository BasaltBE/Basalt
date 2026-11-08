using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Crafting;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors;

public sealed class FurnaceBlockActor : BlockActor {
    private const int InputSlot = 0;
    private const int FuelSlot = 1;
    private const int ResultSlot = 2;
    private const int CookProgressProperty = 0;
    private const int BurnTimeProperty = 1;
    private const int BurnDurationProperty = 2;

    private static readonly (string Type, string Actor, ContainerEnumName Ingredient, ContainerType Container, string Unlit, string Lit)[] Variants = [
        ("furnace", "Furnace", ContainerEnumName.FurnaceIngredient, ContainerType.Furnace,
            "minecraft:furnace", "minecraft:lit_furnace"),
        ("blast_furnace", "BlastFurnace", ContainerEnumName.BlastFurnaceIngredient, ContainerType.BlastFurnace,
            "minecraft:blast_furnace", "minecraft:lit_blast_furnace"),
        ("smoker", "Smoker", ContainerEnumName.SmokerIngredient, ContainerType.Smoker,
            "minecraft:smoker", "minecraft:lit_smoker")
    ];

    private readonly int variant;

    public readonly BlockContainer Container;
    public readonly string FurnaceType;
    private int lastResultCount;
    public int BurnTime;
    public int BurnDuration;
    public int CookTime;
    public int StoredExperience;
    public override string ActorIdentifier => Variants[variant].Actor;
    public override Container? Inventory => Container;

    public override bool CanInsert(int slot, ItemStack item, byte face) {
        if (face == 1) return slot == InputSlot && Container.CanContain(slot, item);
        if (face == 0 || face >= 2) return slot == FuelSlot && Container.CanContain(slot, item);
        return false;
    }

    public override bool CanExtract(int slot, ItemStack item, byte face) {
        if (face == 0) return slot == ResultSlot || slot == FuelSlot && item.Type.Identifier == "minecraft:bucket";
        if (face >= 2) return slot == FuelSlot;
        return false;
    }

    public FurnaceBlockActor(BlockActorContext context) : base(context) {
        variant = context.Identifier.Contains("blast_furnace", StringComparison.Ordinal)
            ? 1
            : context.Identifier.Contains("smoker", StringComparison.Ordinal) ? 2 : 0;
        var definition = Variants[variant];
        FurnaceType = definition.Type;

        Container = new FurnaceContainer(
            context.Position,
            3,
            ContainerId.First,
            definition.Ingredient,
            definition.Container,
            FurnaceType,
            [definition.Ingredient, ContainerEnumName.FurnaceFuel, ContainerEnumName.FurnaceResult]
        );
        Container.Changed += () => {
            int resultCount = Container.GetItem(ResultSlot)?.StackSize ?? 0;
            if (resultCount < lastResultCount && StoredExperience > 0) {
                Dimension.World?.Server?.ExperienceDrops.Drop(Dimension, new Vec3 {
                    X = Position.X + 0.5f,
                    Y = Position.Y + 0.5f,
                    Z = Position.Z + 0.5f
                }, StoredExperience);
                StoredExperience = 0;
            }

            lastResultCount = resultCount;
            MarkDirty();
        };
    }

    public override void OnRead(CompoundTag nbt) {
        BurnTime = Math.Max(0, (int)(nbt.Get<ShortTag>("BurnTime")?.Value ?? 0));
        BurnDuration = Math.Max(0, (int)(nbt.Get<ShortTag>("BurnDuration")?.Value ?? 0));
        CookTime = Math.Max(0, (int)(nbt.Get<ShortTag>("CookTime")?.Value ?? 0));
        StoredExperience = Math.Max(0, nbt.Get<IntTag>("StoredXPInt")?.Value ?? 0);

        ReadItems(nbt, Container);
        lastResultCount = Container.GetItem(ResultSlot)?.StackSize ?? 0;
    }

    public override void OnWrite(CompoundTag nbt) {
        nbt.Add("BlockEntityVersion", new IntTag { Value = 0 });
        nbt.Add("BurnTime", new ShortTag { Value = checked((short)BurnTime) });
        nbt.Add("BurnDuration", new ShortTag { Value = checked((short)BurnDuration) });
        nbt.Add("CookTime", new ShortTag { Value = checked((short)CookTime) });
        nbt.Add("StoredXPInt", new IntTag { Value = StoredExperience });
        nbt.Add("Items", WriteItems(Container));
    }

    public override void OnInteract(Player player) {
        if (!Container.Open(player)) return;

        SendProgress(player);
    }

    public override void OnBreak(Player player) {
        StoredExperience = 0;

        for (int slot = 0; slot < Container.ContainerSize; slot++) {
            if (Container.TakeItem(slot, ushort.MaxValue) is not { } item) continue;

            new ItemEntity(item, player, Position.X, Position.Y, Position.Z).Spawn(Dimension);
        }
    }

    public override void OnRemove() => Container.CloseViewers();

    public override void Tick() {
        int previousBurnTime = BurnTime;
        int previousBurnDuration = BurnDuration;
        int previousCookTime = CookTime;
        int previousExperience = StoredExperience;
        ItemStack? input = Container.GetItem(InputSlot);
        ItemStack? fuel = Container.GetItem(FuelSlot);
        ItemStack? output = Container.GetItem(ResultSlot);
        CookingRecipe? recipe = CookingPalette.Find(input, FurnaceType);
        bool canCook = CanCook(recipe, output);
        bool wasBurning = BurnTime > 0;

        if (BurnTime > 0) BurnTime--;

        int burnTime = CookingPalette.GetBurnTime(fuel);
        if (BurnTime == 0 && canCook && fuel is { } fuelItem && burnTime > 0) {
            BurnTime = burnTime;
            BurnDuration = burnTime;
            if (fuelItem.Type.Identifier == "minecraft:lava_bucket" && fuelItem.StackSize == 1)
                Container.SetItem(FuelSlot, ItemPalette.Find("minecraft:bucket") is { } bucket ? new ItemStack(bucket, 1) : null);
            else Container.TakeItem(FuelSlot);
        }

        if (BurnTime > 0 && canCook) {
            CookTime++;
            if (CookTime >= (FurnaceType == "furnace" ? 200 : 100)) {
                Cook(recipe!.Value, input!, output);
                CookTime = 0;
            }
        } else CookTime = 0;

        bool burning = BurnTime > 0;
        if (!burning) BurnDuration = 0;
        if (burning != wasBurning) SetLit(burning);

        if (Container.HasViewers && (Container.GetItem(InputSlot) is not null || Container.GetItem(FuelSlot) is not null || burning || wasBurning))
            SendProgressToViewers();
        if (BurnTime != previousBurnTime || BurnDuration != previousBurnDuration ||
            CookTime != previousCookTime || StoredExperience != previousExperience)
            MarkDirty();
    }

    sealed class FurnaceContainer(
        BlockPos position,
        int size,
        ContainerId containerId,
        ContainerEnumName containerName,
        ContainerType containerType,
        string furnaceType,
        ContainerEnumName[] slotNames
    ) : BlockContainer(position, size, containerId, containerName, containerType, slotNames: slotNames) {
        public override bool CanContain(int slot, ItemStack item) {
            if (slot == InputSlot) return CookingPalette.Find(item, furnaceType) is not null;
            if (slot == FuelSlot) return CookingPalette.GetBurnTime(item) > 0;
            return false;
        }
    }

    private bool CanCook(CookingRecipe? recipe, ItemStack? output) {
        if (recipe is not { } cooking) return false;
        if (output is null) return cooking.Count <= cooking.Result.MaxAmount;
        return output.Type == cooking.Result && output.Metadata == cooking.Metadata &&
            output.StackSize + cooking.Count <= output.Type.MaxAmount;
    }

    private void Cook(CookingRecipe recipe, ItemStack input, ItemStack? output) {
        if (output is null) Container.SetItem(ResultSlot, new ItemStack(recipe.Result, recipe.Count, recipe.Metadata));
        else output.IncrementStack(recipe.Count);

        Container.TakeItem(InputSlot);

        float experience = Dimension.World?.Server?.ExperienceDrops.GetSmeltExperience(recipe.Result.Identifier) * recipe.Count ?? 0;
        StoredExperience += (int)MathF.Floor(experience);
        if (Random.Shared.NextSingle() < experience - MathF.Floor(experience)) StoredExperience++;
    }

    private void SetLit(bool lit) {
        var definition = Variants[variant];
        string targetIdentifier = lit ? definition.Lit : definition.Unlit;
        BlockPermutation current = Dimension.GetPermutation(Position.X, Position.Y, Position.Z);
        if (current.Identifier == targetIdentifier ||
            BlockPalette.Find(targetIdentifier, current.State) is not { } target ||
            Dimension.GetChunk(Position.X >> 4, Position.Z >> 4) is not { } chunk)
            return;

        chunk.SetPermutation(Position.X & 15, Position.Y, Position.Z & 15, target);

        Dimension.Broadcast(new UpdateBlockPacket {
            Position = Position,
            BlockRuntimeId = unchecked((uint)target.NetworkId),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, Position.X >> 4, Position.Z >> 4);
    }

    private void SendProgress(Player player) {
        player.SendPacket(new ContainerSetDataPacket {
            ContainerId = unchecked((byte)(sbyte)Container.ContainerId),
            Property = CookProgressProperty,
            Value = CookTime
        });
        player.SendPacket(new ContainerSetDataPacket {
            ContainerId = unchecked((byte)(sbyte)Container.ContainerId),
            Property = BurnTimeProperty,
            Value = BurnTime
        });
        player.SendPacket(new ContainerSetDataPacket {
            ContainerId = unchecked((byte)(sbyte)Container.ContainerId),
            Property = BurnDurationProperty,
            Value = BurnDuration
        });
    }

    private void SendProgressToViewers() {
        foreach (Player player in Dimension.GetPlayers()) {
            if (!player.Disposed && ReferenceEquals(player.OpenedContainer, Container)) SendProgress(player);
        }
    }
}
