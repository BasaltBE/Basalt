namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities;
using Basalt.Core.Item;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class HopperTrait : BlockTrait
{
  public static new readonly string Identifier = "minecraft:hopper";
  public static new readonly string[] Types = ["minecraft:hopper"];

  private const int HopperSize = 5;
  private const int TransferCooldown = 8;

  private BlockContainer? _container;
  private int _transferCooldown;
  private bool _ticking;

  public HopperTrait(Block block) : base(block)
  {
  }

  public BlockContainer? Container => _container;

  public override void OnRead(CompoundTag tag)
  {
    _transferCooldown = tag.Get<IntTag>("TransferCooldown")?.Value ?? 0;

    if (tag.Get<ListTag>("Items") is not { } items) return;

    EnsureContainer(null, 0, 0, 0);
    if (_container is null) return;

    foreach (BaseTag entry in items.Values)
    {
      if (entry is not CompoundTag itemTag) continue;
      int slot = itemTag.Get<ByteTag>("Slot")?.Value ?? -1;
      if (slot < 0 || slot >= _container.GetSize()) continue;

      ItemStack? item = ItemStack.Deserialize(itemTag);
      if (item is not null)
      {
        _container.SetItem(slot, item);
      }
    }
  }

  public override void OnWrite(CompoundTag tag)
  {
    tag.Set("TransferCooldown", new IntTag { Value = _transferCooldown });

    if (_container is null) return;

    ListTag items = new() { Name = "Items" };
    for (int slot = 0; slot < _container.GetSize(); slot++)
    {
      ItemStack? item = _container.GetItem(slot);
      if (item is null || item.StackSize == 0) continue;

      CompoundTag itemTag = item.Serialize();
      itemTag.Set("Slot", new ByteTag { Value = (sbyte)slot });
      items.Values.Add(itemTag);
    }

    if (items.Values.Count > 0)
    {
      tag.Set("Items", items);
    }
  }

  public override void OnPlace(BlockPlaceDetails details)
  {
    var dimension = details.Player.Dimension;
    if (dimension is null) return;

    FacingDirection facing = details.BlockFace switch
    {
      0 => FacingDirection.Down,
      1 => FacingDirection.Down,
      2 => FacingDirection.South,
      3 => FacingDirection.North,
      4 => FacingDirection.East,
      5 => FacingDirection.West,
      _ => FacingDirection.Down
    };

    SetFacingDirection(facing);

    EnsureContainer(
      dimension,
      details.BlockPosition.X,
      details.BlockPosition.Y,
      details.BlockPosition.Z);

    ScheduleTick(dimension, details.BlockPosition);
  }

  public override void OnInteract(BlockInteractDetails details)
  {
    var dimension = details.Player.Dimension;
    if (dimension is null) return;

    EnsureContainer(
      dimension,
      details.BlockPosition.X,
      details.BlockPosition.Y,
      details.BlockPosition.Z);

    _container?.Show(details.Player);
  }

  public override void OnBreak(BlockBreakDetails details)
  {
    if (_container is null) return;

    foreach ((Player.Player player, _) in _container.GetAllOccupants().ToList())
    {
      _container.Close(player);
    }

    var dimension = details.Player.Dimension;
    if (dimension is not null)
    {
      ulong currentTick = dimension.World is Worlds.Tickable tickable ? tickable.TickValue : 0;

      for (int i = 0; i < _container.GetSize(); i++)
      {
        ItemStack? item = _container.GetItem(i);
        if (item is null || item.StackSize == 0) continue;

        ItemEntity drop = new(item)
        {
          Position = new Vec3f
          {
            X = details.BlockPosition.X + 0.5f,
            Y = details.BlockPosition.Y + 0.5f,
            Z = details.BlockPosition.Z + 0.5f
          }
        };

        drop.LockPickupUntil(currentTick + 10);
        drop.Spawn(dimension, new Entities.Traits.Types.EntitySpawnOptions(InitialSpawn: false));
      }
    }

    _container = null;
  }

  public override void OnRender(Player.Player player, int x, int y, int z)
  {
    var dimension = player.Dimension;
    if (dimension is null) return;

    BlockPos position = new() { X = x, Y = y, Z = z };

    EnsureContainer(dimension, x, y, z);
    WriteStorage(dimension, x, y, z);

    BlockLevelStorage? storage = dimension
      .GetChunk(x >> 4, z >> 4)
      ?.GetBlockStorage(position);

    if (storage is null) return;

    uint networkId = (uint)dimension.GetPermutation(x, y, z).NetworkId;

    player.Send(
      new BlockActorDataPacket
      {
        Position = position,
        Data = storage
      },
      new UpdateBlockPacket
      {
        Position = position,
        NetworkBlockId = 0,
        Flags = UpdateBlockFlagsType.None,
        Layer = UpdateBlockLayerType.Normal
      },
      new UpdateBlockPacket
      {
        Position = position,
        NetworkBlockId = networkId,
        Flags = UpdateBlockFlagsType.None,
        Layer = UpdateBlockLayerType.Normal
      });
  }

  /// <summary>
  /// Called each hopper tick. Returns true if the hopper should keep ticking.
  /// </summary>
  public bool Tick()
  {
    if (_container?.Dimension is null) return false;

    if (_transferCooldown > 0)
    {
      _transferCooldown--;
      return true;
    }

    bool didWork = false;

    didWork |= TryPullFromAbove();
    didWork |= TryPushToTarget();

    if (didWork)
    {
      _transferCooldown = TransferCooldown;
    }

    return HasWork();
  }

  public void ScheduleTick(Dimension dimension, BlockPos pos)
  {
    if (_ticking) return;
    _ticking = true;

    Server? server = dimension.World?.Server;
    if (server is null) return;

    server.Scheduler.Schedule(
      new HopperTickTask(dimension, pos) { DelayTicks = 1, RunOnMainThread = true },
      dimension.World!.TickValue);
  }

  public void MarkTickingStopped()
  {
    _ticking = false;
  }

  private bool HasWork()
  {
    if (_container is null) return false;
    return !_container.IsFull || _container.EmptySlotsCount < _container.GetSize();
  }

  private bool TryPullFromAbove()
  {
    if (_container?.Dimension is null) return false;
    if (_container.IsFull) return false;

    BlockPos pos = _container.Position;
    Dimension dimension = _container.Dimension;

    if (TryPullFromContainer(dimension, pos.X, pos.Y + 1, pos.Z))
    {
      return true;
    }

    return TryPullFromItemEntities(dimension, pos);
  }

  private bool TryPullFromContainer(Dimension dimension, int x, int y, int z)
  {
    BlockContainer? source = GetContainerAt(dimension, x, y, z);
    if (source is null) return false;

    for (int slot = 0; slot < source.GetSize(); slot++)
    {
      ItemStack? item = source.GetItem(slot);
      if (item is null || item.StackSize == 0) continue;

      ItemStack? taken = source.TakeItem(slot, 1);
      if (taken is null) continue;

      if (_container!.AddItem(taken))
      {
        return true;
      }

      source.SetItem(slot, taken);
      break;
    }

    return false;
  }

  private bool TryPullFromItemEntities(Dimension dimension, BlockPos hopperPos)
  {
    float minX = hopperPos.X;
    float maxX = hopperPos.X + 1.0f;
    float minZ = hopperPos.Z;
    float maxZ = hopperPos.Z + 1.0f;
    float minY = hopperPos.Y + 0.5f;
    float maxY = hopperPos.Y + 2.0f;

    foreach (Entity entity in dimension.Entities)
    {
      if (entity is not ItemEntity itemEntity || !itemEntity.IsAlive || itemEntity.PendingDespawn)
      {
        continue;
      }

      if (itemEntity.Item.StackSize == 0) continue;

      Vec3f ePos = itemEntity.Position;
      if (ePos.X < minX || ePos.X > maxX ||
          ePos.Z < minZ || ePos.Z > maxZ ||
          ePos.Y < minY || ePos.Y > maxY)
      {
        continue;
      }

      ItemStack clone = itemEntity.Item.Clone(1);
      if (!_container!.AddItem(clone)) continue;

      itemEntity.Item.SetStackSize((ushort)(itemEntity.Item.StackSize - 1));
      if (itemEntity.Item.StackSize == 0)
      {
        itemEntity.Despawn(new Entities.Traits.Types.EntityDespawnOptions());
      }

      return true;
    }

    return false;
  }

  private bool TryPushToTarget()
  {
    if (_container?.Dimension is null) return false;

    bool allEmpty = true;
    for (int i = 0; i < _container.GetSize(); i++)
    {
      if (_container.GetItem(i) is not null)
      {
        allEmpty = false;
        break;
      }
    }

    if (allEmpty) return false;

    Dimension dimension = _container.Dimension;
    BlockPos pos = _container.Position;
    GetOutputPosition(pos, out int tx, out int ty, out int tz);

    BlockContainer? target = GetContainerAt(dimension, tx, ty, tz);
    if (target is null) return false;
    if (target.IsFull) return false;

    for (int slot = 0; slot < _container.GetSize(); slot++)
    {
      ItemStack? item = _container.GetItem(slot);
      if (item is null || item.StackSize == 0) continue;

      ItemStack? taken = _container.TakeItem(slot, 1);
      if (taken is null) continue;

      if (target.AddItem(taken))
      {
        return true;
      }

      _container.SetItem(slot, taken);
      break;
    }

    return false;
  }

  private void GetOutputPosition(BlockPos pos, out int x, out int y, out int z)
  {
    x = pos.X;
    y = pos.Y;
    z = pos.Z;

    FacingDirection direction = GetFacingDirection();

    switch (direction)
    {
      case FacingDirection.Down:
        y--;
        break;
      case FacingDirection.North:
        z--;
        break;
      case FacingDirection.South:
        z++;
        break;
      case FacingDirection.West:
        x--;
        break;
      case FacingDirection.East:
        x++;
        break;
      default:
        y--;
        break;
    }
  }

  private FacingDirection GetFacingDirection()
  {
    if (!Block.Permutation.State.TryGetValue("facing_direction", out BlockStateValue value) || value.Kind != 0)
    {
      return FacingDirection.Down;
    }

    return (FacingDirection)(int)value.AsNumber();
  }

  private void SetFacingDirection(FacingDirection direction)
  {
    BlockState state = [];
    foreach ((string key, BlockStateValue value) in Block.Permutation.State)
    {
      state[key] = value;
    }

    state["facing_direction"] = (int)direction;
    Block.SetPermutation(Block.Type.GetPermutation(state));
  }

  private static BlockContainer? GetContainerAt(Dimension dimension, int x, int y, int z)
  {
    Block? block = dimension.GetBlock(x, y, z);
    if (block is null) return null;

    ChestTrait? chest = block.GetTrait<ChestTrait>();
    if (chest?.Container is not null) return chest.Container;

    BarrelTrait? barrel = block.GetTrait<BarrelTrait>();
    if (barrel?.Container is not null) return barrel.Container;

    HopperTrait? hopper = block.GetTrait<HopperTrait>();
    if (hopper?._container is not null) return hopper._container;

    FurnaceTrait? furnace = block.GetTrait<FurnaceTrait>();
    if (furnace?.Container is not null) return furnace.Container;

    return null;
  }

  private void EnsureContainer(Dimension? dimension, int x, int y, int z)
  {
    if (_container is not null)
    {
      if (dimension is not null && _container.Dimension is null)
      {
        _container.Dimension = dimension;
        _container.Position = new BlockPos { X = x, Y = y, Z = z };
      }
      return;
    }

    _container = new BlockContainer(
      dimension,
      new BlockPos { X = x, Y = y, Z = z },
      ContainerType.Hopper,
      HopperSize);

    _container.OnContainerUpdated = OnContainerUpdated;
  }

  private void OnContainerUpdated(BlockContainer container)
  {
    if (container.Dimension is null) return;

    var chunk = container.Dimension.GetChunk(container.Position.X >> 4, container.Position.Z >> 4);
    if (chunk is not null)
    {
      chunk.Dirty = true;
    }

    if (!_ticking)
    {
      ScheduleTick(container.Dimension, container.Position);
    }
  }

  private void WriteStorage(Dimension dimension, int x, int y, int z)
  {
    var chunk = dimension.GetChunk(x >> 4, z >> 4);
    if (chunk is null) return;

    BlockPos position = new() { X = x, Y = y, Z = z };
    BlockLevelStorage? storage = chunk.GetBlockStorage(position);

    if (storage is null)
    {
      storage = new BlockLevelStorage(chunk);
      storage.SetPosition(position);
      storage.Set("id", new StringTag { Name = "id", Value = "Hopper" });
      storage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
    }

    OnWrite(storage);
    chunk.SetBlockStorage(position, storage, dirty: true);
  }
}
