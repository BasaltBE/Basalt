namespace Basalt.Core.Player.Traits;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Container;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Protocol.Enums;

using Entity = Basalt.Core.Entities.Entity;

/// <summary>
/// The 2x2 crafting grid embedded in the player inventory.
/// </summary>
public sealed class PlayerCraftingGridTrait : PlayerTrait
{
  public new static string Identifier => "crafting_grid";
  public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

  public const int GridSize = 4;
  public const int SlotOffset = 28;

  public EntityContainer Container { get; }

  public PlayerCraftingGridTrait(Entity entity) : base(entity)
  {
    Container = new EntityContainer(Player, ContainerType.None, GridSize);
  }

  public override void OnSpawn(EntitySpawnOptions details)
  {
  }

  public override EntityTrait Clone(Entity entity)
  {
    PlayerCraftingGridTrait clone = new(entity);
    for (int i = 0; i < GridSize; i++)
    {
      if (Container.GetItem(i) is { } item)
      {
        clone.Container.SetItem(i, item);
      }
    }
    return clone;
  }

  /// <summary>
  /// Maps a client slot index (28-31) to a local container index (0-3).
  /// </summary>
  public static int MapSlot(int clientSlot)
  {
    return clientSlot - SlotOffset;
  }
}
