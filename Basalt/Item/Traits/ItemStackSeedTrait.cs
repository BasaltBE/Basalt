namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;


public sealed class ItemStackSeedTrait : ItemTrait {
    public new static string Identifier => "seed_plant";
    public new static readonly Type Component = typeof(ItemTypeSeedComponent);

    private static readonly HashSet<string> DefaultPlantableBlocks = new(StringComparer.Ordinal)
    {
        BlockIdentifier.Farmland.ToIdentifier()
    };

    public ItemStackSeedTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        if (details.Player.Dimension is null) return;

        Dimension dimension = details.Player.Dimension;
        BlockPos clickedPos = details.BlockPosition;
        int face = details.BlockFace;

        // Top face
        if (face != 1) return;

        BlockPermutation clickedBlock = dimension.GetPermutation(clickedPos.X, clickedPos.Y, clickedPos.Z);
        if (!IsPlantableBlock(clickedBlock)) return;

        BlockPos cropPos = new() { X = clickedPos.X, Y = clickedPos.Y + 1, Z = clickedPos.Z };
        BlockPermutation above = dimension.GetPermutation(cropPos.X, cropPos.Y, cropPos.Z);
        if (!above.Type.Air) return;

        string cropIdentifier = GetCropIdentifier();
        if (string.IsNullOrEmpty(cropIdentifier)) return;

        BlockType? cropType = BlockType.Get(cropIdentifier);
        if (cropType is null) return;

        BlockPermutation cropPerm = cropType.GetPermutation();
        dimension.SetPermutation(cropPos.X, cropPos.Y, cropPos.Z, cropPerm, 0, true);

        CropTrait.ScheduleCropTick(dimension, cropPos);

        dimension.Broadcast(new LevelSoundEventPacket {
            Event = LevelSoundEvent.Place,
            Position = new Vec3f {
                X = cropPos.X + 0.5f,
                Y = cropPos.Y + 0.5f,
                Z = cropPos.Z + 0.5f
            },
            Data = cropPerm.NetworkId,
            ActorIdentifier = string.Empty,
            BabyMob = false,
            DisableRelativeVolume = false,
            UniqueActorId = 0,
            FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
        });

        if (details.Player.Gamemode == Gamemode.Survival) {
            ItemStack.DecrementStack();

            var inventory = details.Player.GetTrait<Basalt.Core.Entities.Traits.EntityInventoryTrait>();
            if (inventory is not null) {
                if (ItemStack.StackSize == 0) {
                    inventory.Container.ClearSlot(details.HotBarSlot);
                }
                else {
                    inventory.Container.UpdateSlot(details.HotBarSlot);
                }
            }
        }
    }

    private string GetCropIdentifier() {
        ItemTypeSeedComponent? seedComponent = ItemStack.Type.Components.GetComponent<ItemTypeSeedComponent>();
        if (seedComponent is not null) {
            string crop = seedComponent.GetCropResult();
            if (!string.IsNullOrEmpty(crop)) return crop;
        }

        return string.Empty;
    }

    private bool IsPlantableBlock(BlockPermutation perm) {
        ItemTypeSeedComponent? seedComponent = ItemStack.Type.Components.GetComponent<ItemTypeSeedComponent>();
        if (seedComponent is not null) {
            string[] plantAt = seedComponent.GetPlantAt();
            if (plantAt.Length > 0) {
                foreach (string block in plantAt) {
                    if (string.Equals(perm.Type.Identifier, block, StringComparison.Ordinal))
                        return true;
                }
                return false;
            }
        }

        return DefaultPlantableBlocks.Contains(perm.Type.Identifier);
    }
}
