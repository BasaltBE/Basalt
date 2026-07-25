namespace Basalt.Core.Item.Traits;

using Basalt.Core.Blocks.Traits;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

public sealed class ItemStackSpawnEggTrait : ItemTrait {
    public new static string Identifier => "spawn_egg";
    public new static readonly Type Component = typeof(ItemTypeEntityPlacerComponent);

    public ItemStackSpawnEggTrait(ItemStack itemStack) : base(itemStack) {
    }

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        string? entityIdentifier = ResolveEntityIdentifier();
        if (entityIdentifier is null) {
            return;
        }

        BlockPos clickedPosition = details.BlockPosition;
        MobSpawnerTrait? spawner = dimension
            .GetBlock(clickedPosition.X, clickedPosition.Y, clickedPosition.Z)
            ?.GetTrait<MobSpawnerTrait>();

        if (spawner is not null) {
            spawner.Configure(dimension, clickedPosition, entityIdentifier);
            Consume(details);
            return;
        }

        BlockPos spawnBlock = GetSpawnBlock(clickedPosition, details.BlockFace);
        Entity entity = new(entityIdentifier) {
            Position = new Vec3f {
                X = spawnBlock.X + 0.5f,
                Y = spawnBlock.Y,
                Z = spawnBlock.Z + 0.5f
            }
        };

        entity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
        Consume(details);
    }

    private string? ResolveEntityIdentifier() {
        ItemTypeEntityPlacerComponent? entityPlacer =
            ItemStack.Type.Components.GetComponent<ItemTypeEntityPlacerComponent>();
        if (entityPlacer is null) {
            return null;
        }

        string entityIdentifier = entityPlacer.GetEntity();
        return EntityType.Get(entityIdentifier) is null ? null : entityIdentifier;
    }

    private void Consume(ItemUseOnBlockDetails details) {
        if (details.Player.Gamemode != Gamemode.Survival) {
            return;
        }

        ItemStack.DecrementStack();

        EntityInventoryTrait? inventory = details.Player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) {
            return;
        }

        if (ItemStack.StackSize == 0) {
            inventory.Container.ClearSlot(details.HotBarSlot);
        }
        else {
            inventory.Container.UpdateSlot(details.HotBarSlot);
        }
    }

    private static BlockPos GetSpawnBlock(BlockPos clicked, int face) {
        return face switch {
            0 => new BlockPos { X = clicked.X, Y = clicked.Y - 1, Z = clicked.Z },
            1 => new BlockPos { X = clicked.X, Y = clicked.Y + 1, Z = clicked.Z },
            2 => new BlockPos { X = clicked.X, Y = clicked.Y, Z = clicked.Z - 1 },
            3 => new BlockPos { X = clicked.X, Y = clicked.Y, Z = clicked.Z + 1 },
            4 => new BlockPos { X = clicked.X - 1, Y = clicked.Y, Z = clicked.Z },
            5 => new BlockPos { X = clicked.X + 1, Y = clicked.Y, Z = clicked.Z },
            _ => new BlockPos { X = clicked.X, Y = clicked.Y + 1, Z = clicked.Z }
        };
    }
}
