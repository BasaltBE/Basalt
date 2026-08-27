namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Core.Tasks;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class EntityLavaTrait : EntityTrait {
    internal const float Damage = 4f;
    private const ulong DamageInterval = 10;
    private const ulong ItemCheckInterval = 5;

    private static readonly ItemIdentifier[] LavaProofItems = [
        ItemIdentifier.NetheriteAxe,
        ItemIdentifier.NetheriteBlock,
        ItemIdentifier.NetheriteBoots,
        ItemIdentifier.NetheriteChestplate,
        ItemIdentifier.NetheriteHelmet,
        ItemIdentifier.NetheriteHoe,
        ItemIdentifier.NetheriteIngot,
        ItemIdentifier.NetheriteLeggings,
        ItemIdentifier.NetheritePickaxe,
        ItemIdentifier.NetheriteScrap,
        ItemIdentifier.NetheriteShovel,
        ItemIdentifier.NetheriteSword,
    ];

    public new static string Identifier => "lava";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Item, EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:lava_movement"];

    public EntityLavaTrait(Entity entity) : base(entity) {
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null) {
            return;
        }

        if (Entity is ItemEntity) {
            if (details.CurrentTick % ItemCheckInterval != 0) {
                return;
            }
        }
        else if (details.CurrentTick % DamageInterval != 0) {
            return;
        }

        Dimension dimension = Entity.Dimension;
        EntityCollisionTrait? collision = Entity.GetTrait<EntityCollisionTrait>();
        float width = collision?.Width ?? EntityCollisionTrait.DefaultWidth;
        float height = collision?.Height ?? EntityCollisionTrait.DefaultHeight;
        Vec3 location = Entity.GetFeetPosition();
        const float epsilon = 0.001f;
        int minX = (int)MathF.Floor(location.X - width * 0.5f + epsilon);
        int maxX = (int)MathF.Floor(location.X + width * 0.5f - epsilon);
        int minY = (int)MathF.Floor(location.Y - epsilon);
        int maxY = (int)MathF.Floor(location.Y + height - epsilon);
        int minZ = (int)MathF.Floor(location.Z - width * 0.5f + epsilon);
        int maxZ = (int)MathF.Floor(location.Z + width * 0.5f - epsilon);

        LavaCheckTask task = new(
            Entity,
            location,
            dimension,
            minX,
            maxX,
            minY,
            maxY,
            minZ,
            maxZ
        ) {
            CompletionMailbox = dimension.Mailbox
        };
        dimension.World?.Server?.Scheduler.Schedule(task);
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityLavaTrait(entity);
    }

    internal static bool IsLavaProof(ItemEntity item) {
        return LavaProofItems.Any(itemType => itemType.ToIdentifier() == item.Item.Identifier);
    }
}
