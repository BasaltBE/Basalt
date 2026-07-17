namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Containers;
using Basalt.Core.Crafting;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Tasks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class FurnaceTrait : BlockTrait
{
    public static new readonly string Identifier = "furnace";
    public static new readonly string[] Types =
    [
      "minecraft:furnace",
    "minecraft:lit_furnace",
    "minecraft:blast_furnace",
    "minecraft:lit_blast_furnace",
    "minecraft:smoker",
    "minecraft:lit_smoker"
    ];

    private const int SlotInput = 0;
    private const int SlotFuel = 1;
    private const int SlotResult = 2;

    private BlockContainer? _container;
    private int _burnTime;
    private int _maxBurnTime;
    private int _cookTime;
    private bool _ticking;

    public BlockContainer? Container => _container;

    public FurnaceTrait(Block block) : base(block)
    {
    }

    public override void OnRead(CompoundTag tag)
    {
        _burnTime = tag.Get<ShortTag>("BurnTime")?.Value ?? 0;
        _cookTime = tag.Get<ShortTag>("CookTime")?.Value ?? 0;
        _maxBurnTime = tag.Get<ShortTag>("MaxTime")?.Value ?? 0;

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
        tag.Set("BurnTime", new ShortTag { Value = (short)_burnTime });
        tag.Set("CookTime", new ShortTag { Value = (short)_cookTime });
        tag.Set("MaxTime", new ShortTag { Value = (short)_maxBurnTime });

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

        EnsureContainer(
          dimension,
          details.BlockPosition.X,
          details.BlockPosition.Y,
          details.BlockPosition.Z);
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
        SendProgressToPlayer(details.Player);
    }

    public override void OnRender(Player.Player player, int x, int y, int z)
    {
        var dimension = player.Dimension;
        if (dimension is null) return;

        EnsureContainer(dimension, x, y, z);
        WriteStorage(dimension, x, y, z);

        BlockPos position = new() { X = x, Y = y, Z = z };
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

    public override void OnBreak(BlockBreakDetails details)
    {
        if (_container is null) return;

        foreach ((Player.Player player, _) in _container.GetAllOccupants().ToList())
        {
            _container.Close(player);
        }

        for (int i = 0; i < _container.GetSize(); i++)
        {
            ItemStack? item = _container.GetItem(i);
            if (item is null || item.StackSize == 0) continue;

            _container.ClearSlot(i);
            details.Player.DropItem(item);
        }

        _container = null;
    }

    public bool Tick()
    {
        if (_container is null) return false;

        ItemStack? input = _container.GetItem(SlotInput);
        ItemStack? fuel = _container.GetItem(SlotFuel);
        ItemStack? result = _container.GetItem(SlotResult);

        FurnaceRecipe? recipe = FindRecipe(input);
        bool canSmelt = CanSmelt(recipe, input, result);

        if (_burnTime <= 0 && canSmelt && fuel is not null && FuelRegistry.IsFuel(fuel))
        {
            ConsumeFuel(fuel);
        }

        bool active = false;

        if (_burnTime > 0)
        {
            _burnTime--;

            if (canSmelt)
            {
                _cookTime++;
                if (_cookTime >= GetCookDuration())
                {
                    SmeltItem(recipe!, input!, result);
                    _cookTime = 0;
                }
            }
            else
            {
                _cookTime = 0;
            }

            active = true;
        }
        else
        {
            _cookTime = 0;
            _maxBurnTime = 0;
        }

        SyncProgressToViewers();
        UpdateLitState(active);

        _ticking = active;
        return active;
    }

    public void ScheduleTick(Dimension dimension, BlockPos pos)
    {
        if (_ticking) return;
        _ticking = true;

        Server? server = dimension.World?.Server;
        if (server is null) return;

        server.Scheduler.Schedule(
          new FurnaceTickTask(dimension, pos) { DelayTicks = 1, RunOnMainThread = true },
          dimension.World!.TickValue);
    }

    private void ConsumeFuel(ItemStack fuel)
    {
        int burnTime = FuelRegistry.GetBurnTime(fuel);
        int speed = GetSpeedMultiplier();
        _maxBurnTime = (int)Math.Ceiling((double)burnTime / speed);
        _burnTime = _maxBurnTime;

        fuel.DecrementStack(1);
        if (fuel.StackSize == 0)
        {
            _container!.ClearSlot(SlotFuel);
        }
        else
        {
            _container!.UpdateSlot(SlotFuel);
        }
    }

    private void SmeltItem(FurnaceRecipe recipe, ItemStack input, ItemStack? result)
    {
        ItemType? outputType = ItemType.Get(recipe.OutputItem)
          ?? ItemType.Get("minecraft:" + recipe.OutputItem);

        if (outputType is null) return;

        if (result is not null && result.StackSize > 0)
        {
            result.IncrementStack(1);
            _container!.UpdateSlot(SlotResult);
        }
        else
        {
            _container!.SetItem(SlotResult, new ItemStack(outputType, 1));
        }

        input.DecrementStack(1);
        if (input.StackSize == 0)
        {
            _container!.ClearSlot(SlotInput);
        }
        else
        {
            _container!.UpdateSlot(SlotInput);
        }
    }

    private FurnaceRecipe? FindRecipe(ItemStack? input)
    {
        if (input is null || input.StackSize == 0) return null;
        string tag = GetFurnaceTag();
        return FurnaceRegistry.Instance.GetRecipe(input.Type.Identifier, tag);
    }

    private static bool CanSmelt(FurnaceRecipe? recipe, ItemStack? input, ItemStack? result)
    {
        if (recipe is null || input is null || input.StackSize == 0) return false;

        if (result is null || result.StackSize == 0) return true;

        ItemType? outputType = ItemType.Get(recipe.OutputItem)
          ?? ItemType.Get("minecraft:" + recipe.OutputItem);

        if (outputType is null) return false;

        return string.Equals(result.Type.Identifier, outputType.Identifier, StringComparison.Ordinal)
          && result.StackSize < result.Type.MaxStackSize;
    }

    private void SyncProgressToViewers()
    {
        if (_container is null) return;

        foreach ((Player.Player player, ContainerId containerId) in _container.GetAllOccupants())
        {
            if (!player.Spawned) continue;
            SendProgress(player, containerId);
        }
    }

    private void SendProgressToPlayer(Player.Player player)
    {
        if (_container is null) return;
        if (!_container.occupants.TryGetValue(player, out ContainerId containerId)) return;
        SendProgress(player, containerId);
    }

    private void SendProgress(Player.Player player, ContainerId containerId)
    {
        player.Send(new ContainerSetDataPacket
        {
            ContainerId = containerId,
            Property = ContainerSetDataPacket.FurnaceTickCount,
            Value = _cookTime
        });

        int litTime = _maxBurnTime > 0
          ? (int)Math.Ceiling((double)_burnTime / _maxBurnTime * 200)
          : 0;

        player.Send(new ContainerSetDataPacket
        {
            ContainerId = containerId,
            Property = ContainerSetDataPacket.FurnaceLitTime,
            Value = litTime
        });
    }

    private void UpdateLitState(bool shouldBeLit)
    {
        if (_container?.Dimension is null) return;

        string currentId = Block.Type.Identifier;
        string targetId = GetTargetBlockId(shouldBeLit);

        if (string.Equals(currentId, targetId, StringComparison.Ordinal)) return;

        Dimension dimension = _container.Dimension;
        BlockPos pos = _container.Position;

        BlockPermutation? target = BlockPermutation.Resolve(targetId, Block.Permutation.State);
        if (target is null) return;

        var chunk = dimension.GetChunk(pos.X >> 4, pos.Z >> 4);
        if (chunk is null) return;

        int lx = pos.X & 0xF;
        int lz = pos.Z & 0xF;
        chunk.SetPermutation(lx, pos.Y, lz, target, layer: 0, dirty: true);

        dimension.Broadcast(new UpdateBlockPacket
        {
            Position = pos,
            NetworkBlockId = (uint)target.NetworkId,
            Flags = UpdateBlockFlagsType.Network,
            Layer = UpdateBlockLayerType.Normal
        });
    }

    private string GetTargetBlockId(bool lit)
    {
        string id = Block.Type.Identifier;

        if (id.Contains("blast_furnace", StringComparison.Ordinal))
        {
            return lit
              ? BlockIdentifier.LitBlastFurnace.ToIdentifier()
              : BlockIdentifier.BlastFurnace.ToIdentifier();
        }

        if (id.Contains("smoker", StringComparison.Ordinal))
        {
            return lit
              ? BlockIdentifier.LitSmoker.ToIdentifier()
              : BlockIdentifier.Smoker.ToIdentifier();
        }

        return lit
          ? BlockIdentifier.LitFurnace.ToIdentifier()
          : BlockIdentifier.Furnace.ToIdentifier();
    }

    private string GetFurnaceTag()
    {
        string id = Block.Type.Identifier;
        if (id.Contains("blast_furnace", StringComparison.Ordinal)) return "blast_furnace";
        if (id.Contains("smoker", StringComparison.Ordinal)) return "smoker";
        return "furnace";
    }

    private int GetCookDuration()
    {
        return 200 / GetSpeedMultiplier();
    }

    private int GetSpeedMultiplier()
    {
        string id = Block.Type.Identifier;
        if (id.Contains("blast_furnace", StringComparison.Ordinal)) return 2;
        if (id.Contains("smoker", StringComparison.Ordinal)) return 2;
        return 1;
    }

    private ContainerType GetContainerType()
    {
        string id = Block.Type.Identifier;
        if (id.Contains("blast_furnace", StringComparison.Ordinal)) return ContainerType.BlastFurnace;
        if (id.Contains("smoker", StringComparison.Ordinal)) return ContainerType.Smoker;
        return ContainerType.Furnace;
    }

    private void EnsureContainer(Dimension? dimension, int x, int y, int z)
    {
        if (_container is not null)
        {
            if (dimension is not null && _container.Dimension is null)
            {
                _container.Dimension = dimension;
                _container.Position = new BlockPos { X = x, Y = y, Z = z };
                if (!_ticking && _burnTime > 0)
                {
                    ScheduleTick(dimension, _container.Position);
                }
            }
            return;
        }

        _container = new BlockContainer(
          dimension,
          new BlockPos { X = x, Y = y, Z = z },
          GetContainerType(),
          3);

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

        if (_ticking) return;

        ItemStack? input = container.GetItem(SlotInput);
        ItemStack? fuel = container.GetItem(SlotFuel);
        ItemStack? result = container.GetItem(SlotResult);
        FurnaceRecipe? recipe = FindRecipe(input);

        bool canStart = (_burnTime > 0 && CanSmelt(recipe, input, result))
          || (CanSmelt(recipe, input, result) && fuel is not null && FuelRegistry.IsFuel(fuel));

        if (canStart)
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
            storage.Set("id", new StringTag { Name = "id", Value = "Furnace" });
            storage.Set("isMovable", new ByteTag { Name = "isMovable", Value = 1 });
        }

        OnWrite(storage);
        chunk.SetBlockStorage(position, storage, dirty: true);
    }
}
