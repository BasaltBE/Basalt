namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Traits;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class ItemStackBucketFillTrait : ItemTrait
{
  public new static string Identifier => "bucket_fill";
  public new static readonly string[] Types =
  [
    ItemIdentifier.Bucket.ToIdentifier()
  ];

  public ItemStackBucketFillTrait(ItemStack itemStack) : base(itemStack)
  {
  }

  public override void OnUseOnBlock(ItemUseOnBlockDetails details)
  {
    if (details.Player.Dimension is null) return;

    Dimension dimension = details.Player.Dimension;
    BlockPos clickedPos = details.BlockPosition;

    BlockPermutation perm = dimension.GetPermutation(clickedPos.X, clickedPos.Y, clickedPos.Z);

    FluidKind? kind = FluidTrait.GetFluidKind(perm);
    if (!kind.HasValue) return;

    if (!FluidTrait.IsSourceBlock(kind.Value, perm)) return;

    BlockPermutation air = BlockPermutation.Resolve(BlockIdentifier.Air.ToIdentifier());
    dimension.RemoveBlock(clickedPos.X, clickedPos.Y, clickedPos.Z);
    dimension.SetPermutation(clickedPos.X, clickedPos.Y, clickedPos.Z, air);

    string soundEvent = kind.Value == FluidKind.Water
      ? LevelSoundEvent.BucketFillWater
      : LevelSoundEvent.BucketFillLava;

    dimension.Broadcast(new LevelSoundEventPacket
    {
      Event = soundEvent,
      Position = new Vec3f
      {
        X = clickedPos.X + 0.5f,
        Y = clickedPos.Y + 0.5f,
        Z = clickedPos.Z + 0.5f
      },
      Data = perm.NetworkId,
      ActorIdentifier = string.Empty,
      BabyMob = false,
      DisableRelativeVolume = false,
      UniqueActorId = 0,
      FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
    });

    if (details.Player.Gamemode == Gamemode.Survival)
    {
      ItemIdentifier filledBucket = kind.Value == FluidKind.Water
        ? ItemIdentifier.WaterBucket
        : ItemIdentifier.LavaBucket;

      ItemType? filledType = ItemType.Get(filledBucket.ToIdentifier());
      if (filledType is null) return;

      var inventory = details.Player.GetTrait<Basalt.Core.Entities.Traits.EntityInventoryTrait>();
      if (inventory is null) return;

      if (ItemStack.StackSize > 1)
      {
        inventory.Container.SetItem(details.HotBarSlot, new ItemStack(ItemStack.Type, (ushort)(ItemStack.StackSize - 1)));
        inventory.Container.AddItem(new ItemStack(filledType, 1));
      }
      else
      {
        inventory.Container.SetItem(details.HotBarSlot, new ItemStack(filledType, 1));
      }
    }
  }
}
